#ifndef _DB_H_
#define _DB_H_

#include "platform_defs.h"
#include <string>
#include "sqlite3.h"

using namespace std;

#define DB_COLUMN_TYPE_VARCHAR   0
#define DB_COLUMN_TYPE_INTEGER   1
#define DB_COLUMN_TYPE_DOUBLE    2
#define DB_COLUMN_TYPE_NVARCHAR  3

class SQLITERecordSet
{
private:
	bool m_failed;
	int m_column_count;
	bool m_initialized;
	bool m_finalized;
	sqlite3_stmt* m_stmt;
	bool m_valid_row;
public:
	SQLITERecordSet(sqlite3* db, wstring select);
	~SQLITERecordSet();
	bool Failed();
	int ColumnCount();
	wstring GetColumnName(int col);
	int GetColumnType(int col);
	bool ColumnNameExists(wstring column_name);
	bool GotoNextRow();
	bool Reset();
	bool BindParameterAsInt(int paramIdx, int param);
	bool BindParameterAsDouble(int paramIdx, double param);
	bool BindParameterAsString(int paramIdx, wstring param);
	int GetColumnValueAsInt(int col);
	double GetColumnValueAsDouble(int col);
	wstring GetColumnValueAsString(int col);
};

class SQLITEDatabase
{
private:
	sqlite3* m_db;
	bool m_db_opened;
	wstring m_last_error;
public:
	SQLITEDatabase();
	bool Open(wstring database);
	bool ExecuteSQLStatement(wstring select);
	SQLITERecordSet* GetRecordSet(wstring select);
	bool Close();
};
#endif