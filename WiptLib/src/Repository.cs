using System;
using System.Text;
using System.Xml;

namespace ACM.Wipt
{
  public class Repository
  {
    XmlDocument m_doc;

    private Repository()
    {
    }

    public Repository(string xmlFile)
    {
      XmlTextReader ready = new XmlTextReader(xmlFile);
      m_doc = new XmlDocument();
      m_doc.Load(ready);
      ready.Close();
    }

    public static Repository Create(string xmlFile, string maintainer, string supportURL)
    {
      string formatstring = "<?xml version=\"1.0\" encoding=\"utf-8\" ?>\r\n<Repository xmlns=\"urn:xmlns:sigwin:wipt-get:repository\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:SchemaLocation=\"urn:xmlns:sigwin:wipt-get:repository http://www.acm.uiuc.edu/sigwin/WiptSchema.xsd\" Maintainer=\"{0}\" SupportURL=\"{1}\"></Repository>";

      XmlDocument newDoc = new XmlDocument();
      newDoc.LoadXml(string.Format(formatstring, maintainer, supportURL));

      XmlTextWriter tex = new XmlTextWriter(xmlFile, Encoding.UTF8);
      tex.Formatting = Formatting.Indented;
      tex.Indentation = 2;
      tex.IndentChar = ' ';
      newDoc.WriteTo(tex);
      tex.Close();

      return new Repository(xmlFile);
    }

    public void Save(string xmlFile)
    {
      XmlTextWriter tex = new XmlTextWriter(xmlFile, Encoding.UTF8);
      tex.Formatting = Formatting.Indented;
      tex.Indentation = 2;
      tex.IndentChar = ' ';

      m_doc.WriteTo(tex);
      tex.Close();
    }

    public bool AddPackage(string productName, Guid upgradeCode, string publisher, string supportURL, string version,
        Guid productCode, string URL, bool makestable)
    {
      string formatstring = "<Package xmlns=\"urn:xmlns:sigwin:wipt-get:repository\" ProductCode=\"{0}\">\r\n      <Version Major=\"{1}\" Minor=\"{2}\" Build=\"{3}\" />\r\n      <URL>{4}</URL>\r\n    </Package>";
      XmlDocumentFragment frag = m_doc.CreateDocumentFragment();
      string[] vp = version.Split('.');
      string[] parts = new string[3]{"0","0","0"};
      Array.Copy(vp, parts, vp.Length);
      frag.InnerXml = string.Format(formatstring, "{" + productCode.ToString().ToUpper() + "}", parts[0], parts[1], parts[2], URL);

      string formatstring2 = "<Product xmlns=\"urn:xmlns:sigwin:wipt-get:repository\" Name=\"{0}\" UpgradeCode=\"{1}\" Publisher=\"{2}\" SupportURL=\"{3}\" ><StableVersion Major=\"{4}\" Minor=\"{5}\" Build=\"{6}\" /></Product>";
      XmlDocumentFragment frag2 = m_doc.CreateDocumentFragment();
      frag2.InnerXml = string.Format(formatstring2, productName, "{" + upgradeCode.ToString().ToUpper() + "}", publisher, supportURL, parts[0], parts[1], parts[2]);

      foreach(XmlLinkedNode l in m_doc.ChildNodes)
      {
        if(l.Name == "Repository")
        {
          foreach(XmlElement t in l.ChildNodes)
          {
            if(t.Name == "Product")
            {
              if(t.Attributes["UpgradeCode"].Value == "{" + upgradeCode.ToString().ToUpper() + "}")
              {
                if(makestable)
                {
                  foreach(XmlElement g in l.ChildNodes)
                  {
                    if(g.Name == "StableVersion")
                    {
                      SetVersion(g, version);
                      break;
                    }
                  }
                }

                XmlNode node = t.AppendChild(frag);
icky1:
                foreach(XmlAttribute attr in node.Attributes)
                {
                  if(attr.Value == "")
                  {
                    node.Attributes.Remove(attr);
                    goto icky1;
                  }
                }
                return true;
              }
            }
          }

          XmlNode nod = l.AppendChild(frag2);
          XmlNode nodey = nod.AppendChild(frag);
icky2:
          foreach(XmlAttribute attr in nod.Attributes)
          {
            if(attr.Value == "")
            {
              nod.Attributes.Remove(attr);
              goto icky2;
            }
          }
icky3:
          foreach(XmlAttribute attr in nodey.Attributes)
          {
            if(attr.Value == "")
            {
              nodey.Attributes.Remove(attr);
              goto icky3;
            }
          }

          return true;
        }
      }
      return false;
    }

    private static void SetVersion(XmlElement eli, string version)
    {
      string[] ver = version.Split('.');
      eli.Attributes["Major"].Value = ver[0];
      eli.Attributes["Minor"].Value = ver[1];
      eli.Attributes["Build"].Value = ver[2];
    }
  }
}
