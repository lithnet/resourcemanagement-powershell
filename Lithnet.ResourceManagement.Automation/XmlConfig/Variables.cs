using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.IO;
using System.Xml;
using System.Collections;

namespace Lithnet.ResourceManagement.Automation
{
    [XmlRoot(ElementName = "Lithnet.ResourceManagement.ConfigSync.Variables")]
    [XmlType("Variables")]
    public class Variables
    {
        [XmlAttribute("import-file")]
        public string FileName { get; set; }

        [XmlElement("Variable")]
        public List<Variable> Items { get; set; }

        internal void LoadFileVariables(string currentPath)
        {
            if (!string.IsNullOrWhiteSpace(this.FileName))
            {
                XmlSerializer s = new XmlSerializer(typeof(Variables));

                string filename;

                if (Path.IsPathRooted(this.FileName))
                {
                    filename = this.FileName;
                }
                else
                {
                    filename = Path.Combine(currentPath, this.FileName);
                }

                StreamReader sr = File.OpenText(filename);
                XmlReader xr = XmlReader.Create(sr);
                Variables variables = (Variables)s.Deserialize(xr);

                variables.LoadFileVariables(currentPath);

                foreach (Variable variable in variables.Items)
                {
                    this.Items.Add(variable);
                }
            }
        }
    }
}