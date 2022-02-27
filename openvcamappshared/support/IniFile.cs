///---------------------------------------------------------------------------
///
///  Flyer ABC
///
///  Copyright (C) ABCosting Produtos e Serviços, 2005
///
///  File:       $Workfile:   IniFiles.cs  $
///
///  Creation:   05/10/2005 by Clovis Henrique Ribeiro
///
///  Revision:   $Revision:   2.0  $
///
///  Contents:   IniFile Class. This class implements all routines
///							 needed to deal ini files
///
///----------------------------------------------------------------------------
using System;
using System.IO;
using System.Text;
using System.Collections;

namespace OpenVCam.Common.Support
{
    /// <summary>
    /// ConfigFile abstract class implements all the functionality required
    /// for writing and reading to/from config file. It desn't implement how
    /// to load and save data to file. 
    /// The internal structure that ConfigFile uses is the old Windows 3.x 
    /// INI Files
    /// 
    /// enherited classes MUST implement load and save in order to make the
    /// class persistent
    /// </summary>
    public abstract class ConfigFile
    {
        //Private variables
        protected ArrayList lines = new ArrayList();
        protected string fileName;
        //Constructor
        public ConfigFile(string filename)
        {
            //Store filename
            fileName = filename;
            //Load it
            Load(filename);
        }
        //Public methods
        public string ReadString(string section, string key, string defaultValue)
        {
            string sectionString = "[" + section.ToUpper() + "]";
            bool insideSection = false;
            string line;
            key = key.ToUpper();
            for (int i = 0; i < lines.Count; i++)
            {
                line = ((string)lines[i]).Trim().ToUpper();
                if (line.Length > 0)
                {
                    //Check if line is our desired section and if so, turn
                    //our flag to true
                    if (line == sectionString)
                        insideSection = true;
                    //If we find open brackets and are inside desired section
                    //it means that we are living it so, turn our flag to false
                    if ((line.Substring(0, 1) == "[") && (line != sectionString))
                        if (insideSection)
                            insideSection = false;
                    //Only process information if we are inside the desired section
                    //and it's not a comment line
                    if (insideSection && (line.Substring(0, 1) != ";"))
                    {
                        if (line.IndexOf(key) == 0)
                        {
                            //Get line again but without "ToUpper()" method
                            line = ((string)lines[i]).Trim();
                            //Return key value
                            return (line.Substring(line.IndexOf("=") + 1));
                        }
                    }
                }
            }
            return (defaultValue);
        }
        public void WriteString(string section, string key, string stringValue)
        {
            string sectionString = "[" + section.ToUpper() + "]";
            bool insideSection = false;
            int sectionLine = -1;
            string line;
            key = key.ToUpper();
            for (int i = 0; i < lines.Count; i++)
            {
                line = ((string)lines[i]).Trim().ToUpper();
                if (line.Length > 0)
                {
                    //Check if line is our desired section and if so, turn
                    //our flag to true
                    if (line == sectionString)
                    {
                        insideSection = true;
                        sectionLine = i;
                    }
                    //If we find open brackets and are inside desired section
                    //it means that we are living it so, turn our flag to false
                    if ((line.Substring(0, 1) == "[") && (line != sectionString))
                        if (insideSection)
                            insideSection = false;
                    //Only process information if we are inside the desired section
                    //and it's not a comment line
                    if (insideSection && (line.Substring(0, 1) != ";"))
                    {
                        if (line.IndexOf(key) == 0)
                        {
                            //Get line again but without "ToUpper()" method
                            line = ((string)lines[i]).Trim();
                            //Return key value
                            lines[i] = line.Substring(0, line.IndexOf("=") + 1) + stringValue;
                            return;
                        }
                    }
                }
            }
            //If we reach this point, that's because the section/key was not found
            //We first check if at least the section exists and if not, create it
            if (sectionLine == -1)
            {
                lines.Add("\r\n[" + section + "]");
                sectionLine = lines.Count - 1;
            }
            //Insert the new key/stringValue pair
            lines.Insert(sectionLine + 1, key + "=" + stringValue);
        }
        public int ReadInteger(string section, string key, int defaultValue)
        {
            string result = this.ReadString(section, key, defaultValue.ToString());
            int intResult;
            try
            {
                intResult = Convert.ToInt32(result);
            }
            catch
            {
                intResult = defaultValue;
            }
            return (intResult);
        }
        public void WriteInteger(string section, string key, int integerValue)
        {
            this.WriteString(section, key, integerValue.ToString());
        }
        public bool ReadBool(string section, string key, bool defaultValue)
        {
            string stringValue = defaultValue ? "1" : "0";
            string result = this.ReadString(section, key, stringValue);
            if (result == "0")
                return (false);
            if (result == "1")
                return (true);
            return (defaultValue);
        }
        public void WriteBool(string section, string key, bool boolValue)
        {
            string stringValue = boolValue ? "1" : "0";
            this.WriteString(section, key, stringValue);
        }
        //Abstract methods
        protected abstract void Load(string filename);
        public abstract bool Save();
    }

    /// <summary>
    ///  IniFile class. Inherits from ConfigFile class and implements
    ///  Load/Save to ini files
    /// </summary>
    public class IniFile : ConfigFile
    {
        //Constructor
        public IniFile(string filename) : base(filename) { }
        //Implmenentation of inherited abstract methods
        protected override void Load(string filename)
        {
            //Check if file actually exists
            if (!File.Exists(filename))
                return;
            //Open the inifile (stream reader)
            StreamReader sr = new StreamReader(filename, Encoding.Default);
            //Load all lines to our ArrayList
            string line = sr.ReadLine();
            while (line != null)
            {
                lines.Add(line);
                line = sr.ReadLine();
            }
            //Close the file (stream reader)
            sr.Close();
        }

        public override bool Save()
        {
            try
            {
                //Create a new file (stream writer)
                StreamWriter sw = new StreamWriter(fileName, false, Encoding.Default);
                try
                {
                    //Write lines to file
                    for (int i = 0; i < lines.Count; i++)
                        sw.WriteLine((string)lines[i]);
                }
                finally
                {
                    //Close file (stream writer)
                    sw.Close();
                }
                return (true);
            }
            catch
            {
                return (false);
            }
        }
    }
    /// <summary>
    ///  XmlFile class. Inherits from ConfigFile class and implements
    ///  Load/Save to xml files
    /// </summary>
    public class XmlFile : ConfigFile
    {
        //Constructor
        public XmlFile(string filename) : base(filename) { }
        //Implmenentation of inherited abstract methods
        protected override void Load(string filename)
        {
            //Check if file actually exists
            if (!File.Exists(filename))
                return;
            //Open the inifile (stream reader)
            StreamReader sr = new StreamReader(filename);
            //Load all lines to our ArrayList
            string line = sr.ReadLine();
            while (line != null)
            {
                //Clean up spaces
                line = line.Trim();
                //Process line just if it's a data line
                if ((line.IndexOf("<?xml") != 0) &&
                    (line.IndexOf("<configuration>") != 0) &&
                    (line.IndexOf("</configuration>") != 0))
                {
                    //If this line isn't an end of section and also isn't a key/value line
                    //by default it's a init section line
                    if ((line.Substring(0, 2) != "</") && (line.IndexOf("/>") < 0))
                        lines.Add("[" + line.Substring(1, line.Length - 2) + "]");
                    if (line.IndexOf("/>") > 0)
                    {
                        string val = line.Substring(line.IndexOf("\"") + 1);
                        val = val.Substring(0, val.IndexOf("\""));
                        line = line.Substring(1, line.IndexOf(" ") - 1) + "=" + val;
                        lines.Add(line);
                    }
                }
                line = sr.ReadLine();
            }
            //Close the file (stream reader)
            sr.Close();
        }
        public override bool Save()
        {
            string line;
            string lineData;
            string currentSection = "";
            try
            {
                //Create a new file (stream writer)
                StreamWriter sw = new StreamWriter(fileName, false);
                try
                {
                    sw.WriteLine("<?xml version = \"1.0\"?>");
                    sw.WriteLine("<configuration>");
                    //Write lines to file
                    for (int i = 0; i < lines.Count; i++)
                    {
                        line = (string)lines[i];
                        if (line.Substring(0, 1) == "[")
                        {
                            //If there's a section opened, close it before creating new one
                            if (currentSection != "")
                                sw.WriteLine("   </" + currentSection + ">");
                            //Create a new section
                            sw.WriteLine("   <" + line.Substring(1, line.Length - 2) + ">");
                            //Store it's name
                            currentSection = line.Substring(1, line.Length - 2);
                        }
                        if ((line.Substring(0, 1) != "[") && (line.Substring(0, 1) != ";"))
                        {
                            lineData = line.Substring(0, line.IndexOf("=")) + " value = \"" + line.Substring(line.IndexOf("=") + 1) + "\"";
                            sw.WriteLine("      <" + lineData + "/>");
                        }
                    }
                    sw.WriteLine("   </" + currentSection + ">");
                    sw.WriteLine("</configuration>");
                }
                finally
                {
                    //Close file (stream writer)
                    sw.Close();
                }
                return (true);
            }
            catch
            {
                return (false);
            }
        }
    }
}
