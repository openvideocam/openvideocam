#include <codecvt>

#ifdef LINUX
#include <bits/locale_conv.h>
#else
#include <Windows.h>
#endif // LINUX

#include <algorithm>
#include "db.h"
#include "platform_defs.h"

using namespace std;

SQLITEDatabase::SQLITEDatabase() {
	m_db_opened = false;
	m_last_error = L"";
	m_db = NULL;
}

bool SQLITEDatabase::Open(wstring database)
{
	//Check if database is already opened
	if (m_db_opened) {
		m_last_error = L"Database already opened";
		return(false);
	}

	//Open database in raw mode
	string database_utf8 = wstring_convert <codecvt_utf8<wchar_t>>().to_bytes(database);
	int rc = sqlite3_open(database_utf8.c_str(), &m_db);
	if (rc) {
		string last_error_ansi = sqlite3_errmsg(m_db);
		m_last_error = std::wstring_convert<std::codecvt_utf8<wchar_t>>().from_bytes(last_error_ansi);
		sqlite3_close(m_db);
		return(false);
	}
	else {
		m_db_opened = true;
		return(true);
	}
}

SQLITERecordSet* SQLITEDatabase::GetRecordSet(wstring select)
{
	return new SQLITERecordSet(m_db, select);
}

bool SQLITEDatabase::ExecuteSQLStatement(wstring select)
{
	if (!m_db_opened) {
		m_last_error = L"No database opened";
		return(false);
	}

	//disable_flush();

	sqlite3_stmt* ppStmt;
	const void* pzTail;
	bool result = false;

#ifdef LINUX
	std::u16string select_u16(select.begin(), select.end());
	if (sqlite3_prepare16_v2(m_db, select_u16.c_str(), -1, &ppStmt, &pzTail) == SQLITE_OK)
#else
	if (sqlite3_prepare16_v2(m_db, select.c_str(), -1, &ppStmt, &pzTail) == SQLITE_OK)
#endif
	{
		result = (sqlite3_step(ppStmt) == SQLITE_DONE);
		sqlite3_finalize(ppStmt);
	}
	return(result);
}

bool SQLITEDatabase::Close()
{
	//Check if database is already opened
	if (!m_db_opened) {
		m_last_error = L"No database opened";
		return(false);
	}
	else {
		//Close database
		sqlite3_close(m_db);
		m_db_opened = false;
		return(true);
	}
}

SQLITERecordSet::SQLITERecordSet(sqlite3* db, wstring select)
{
	//Define helper variables
	const void* zLeftover;

	//Initialize member variables
	m_stmt = 0;
	m_initialized = true;
	m_failed = false;

	//Prepare sql query/statement
#ifdef LINUX
	std::u16string select_u16(select.begin(), select.end());
	if (!sqlite3_prepare16_v2(db, select_u16.c_str(), -1, &m_stmt, &zLeftover) == SQLITE_OK)
#else
	if (!sqlite3_prepare16_v2(db, select.c_str(), -1, &m_stmt, &zLeftover) == SQLITE_OK)
#endif
	{
		//Could not execute query
		m_failed = true;
		if (m_stmt) sqlite3_finalize(m_stmt);
		m_initialized = false;
		return;
	}

	//Get column count
	m_column_count = sqlite3_column_count(m_stmt);
	m_finalized = false;

	//Set valid_row to false as we have not yet called sqlte3_step()
	m_valid_row = false;

}

SQLITERecordSet::~SQLITERecordSet()
{
	if (m_initialized)
		sqlite3_finalize(m_stmt);
}

bool SQLITERecordSet::Failed()
{
	return(m_failed);
}

int SQLITERecordSet::ColumnCount()
{
	if (m_initialized)
		return(m_column_count);
	else
		return(0);
}

wstring SQLITERecordSet::GetColumnName(int col)
{
	wstring result = L"";
	if ((col >= 0) && (col < m_column_count))
	{
		WCHAR* column_name = (WCHAR*)sqlite3_column_name16(m_stmt, col);
		result = column_name;
	}
	return(result);
}

int SQLITERecordSet::GetColumnType(int col)
{
	if ((col >= 0) && (col < m_column_count))
	{
		WCHAR* column_type = (WCHAR*)sqlite3_column_decltype16(m_stmt, col);
		if (column_type != NULL)
		{
			wstring col_type = column_type;
			std::transform(col_type.begin(), col_type.end(), col_type.begin(), ::toupper);
			if (col_type == L"VARCHAR")
				return(DB_COLUMN_TYPE_VARCHAR);
			if (col_type == L"INTEGER")
				return(DB_COLUMN_TYPE_INTEGER);
			if (col_type == L"DOUBLE")
				return(DB_COLUMN_TYPE_DOUBLE);
		}
		else
		{
			//Let's try to use sqlite3_column_type
			if (m_valid_row)
			{
				int column_type = sqlite3_column_type(m_stmt, col);
				if (column_type != SQLITE_NULL)
				{
					if ((column_type == SQLITE_TEXT) || (column_type == SQLITE_BLOB))
						return(DB_COLUMN_TYPE_VARCHAR);
					if (column_type == SQLITE_INTEGER)
						return(DB_COLUMN_TYPE_INTEGER);
					if (column_type == SQLITE_FLOAT)
						return(DB_COLUMN_TYPE_DOUBLE);
				}
			}
		}
	}
	return(DB_COLUMN_TYPE_VARCHAR);
}

bool SQLITERecordSet::ColumnNameExists(wstring column_name)
{
	std::transform(column_name.begin(), column_name.end(), column_name.begin(), ::toupper);
	wstring str_col_name;
	for (int i = 0; i < m_column_count; i++)
	{
		WCHAR* col_name = (WCHAR*)sqlite3_column_name16(m_stmt, i);
		str_col_name = col_name;
		std::transform(str_col_name.begin(), str_col_name.end(), str_col_name.begin(), ::toupper);
		if (str_col_name == column_name)
			return(true);
	}
	return(false);
}

bool SQLITERecordSet::GotoNextRow()
{
	if ((m_initialized) && (!m_finalized))
	{
		//sqlite Step
		int rc = sqlite3_step(m_stmt);
		//If we've got a row, return true...
		if (rc == SQLITE_ROW) {
			if (!m_valid_row)
				m_valid_row = true;
			return(true);
		}
		//...else finalize fastrecordset
		m_valid_row = false;
		sqlite3_reset(m_stmt);
		m_finalized = true;
	}
	return(false);
}

bool SQLITERecordSet::Reset()
{
	if (!m_initialized) return(false);
	sqlite3_reset(m_stmt);
	m_finalized = false;
	return(true);
}

bool SQLITERecordSet::BindParameterAsInt(int paramIdx, int param)
{
	if (!m_initialized) return(false);
	return(sqlite3_bind_int(m_stmt, paramIdx, param) == SQLITE_OK);
}

bool SQLITERecordSet::BindParameterAsDouble(int paramIdx, double param)
{
	if (!m_initialized) return(false);
	return(sqlite3_bind_double(m_stmt, paramIdx, param) == SQLITE_OK);
}

bool SQLITERecordSet::BindParameterAsString(int paramIdx, wstring param)
{
	if (!m_initialized) return(false);
	if (paramIdx > m_column_count) return(false);
	if (param.c_str() == NULL) return(true);
	int sz = (int)wcslen(param.c_str()) * 2;
	bool result = (sqlite3_bind_text16(m_stmt, paramIdx, param.c_str(), sz, SQLITE_TRANSIENT) == SQLITE_OK);
	return(result);
}

int SQLITERecordSet::GetColumnValueAsInt(int col)
{
	return(sqlite3_column_int(m_stmt, col));
}

double SQLITERecordSet::GetColumnValueAsDouble(int col)
{
	double value = sqlite3_column_double(m_stmt, col);

	if ((!(value == 0.0)) && (!(value > 0.0)) && (!(value < 0.0)))
		value = 0.0;

	return(value);
}

wstring SQLITERecordSet::GetColumnValueAsString(int col)
{
	wstring result = L"";
	string result_utf8;
	if (sqlite3_column_text16(m_stmt, col) != NULL)

#ifdef LINUX
		result_utf8 = (char*)(sqlite3_column_text(m_stmt, col));
		std::wstring_convert<std::codecvt_utf8_utf16<wchar_t>> converter;
		result = converter.from_bytes(result_utf8);
#else
		result = (WCHAR*)sqlite3_column_text16(m_stmt, col);
#endif

	return(result);
}