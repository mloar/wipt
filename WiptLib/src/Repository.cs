/*
 *  Copyright (c) 2006 Association for Computing Machinery at the 
 *  University of Illinois at Urbana-Champaign.
 *  All rights reserved.
 * 
 *  Developed by: Special Interest Group for Windows Development
 *                ACM@UIUC
 *                http://www.acm.uiuc.edu/sigwin
 *
 *  Permission is hereby granted, free of charge, to any person obtaining a 
 *  copy of this software and associated documentation files (the "Software"),
 *  to deal with the Software without restriction, including without limitation
 *  the rights to use, copy, modify, merge, publish, distribute, sublicense,
 *  and/or sell copies of the Software, and to permit persons to whom the
 *  Software is furnished to do so, subject to the following conditions:
 *
 *  Redistributions of source code must retain the above copyright notice, this
 *  list of conditions and the following disclaimers.
 *  Redistributions in binary form must reproduce the above copyright notice,
 *  this list of conditions and the following disclaimers in the documentation
 *  and/or other materials provided with the distribution.
 *  Neither the names of SIGWin, ACM@UIUC, nor the names of its contributors
 *  may be used to endorse or promote products derived from this Software
 *  without specific prior written permission. 
 *
 *  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 *  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 *  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 *  CONTRIBUTORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 *  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 *  FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
 *  DEALINGS WITH THE SOFTWARE.
 */

using System;
using System.Text;
using System.Xml;

namespace Acm.Wipt
{
  /// <remarks>Represents a repository file.</remarks>
  public class Repository
  {
    private XmlDocument m_doc;

    private Repository()
    {
    }

    /// <summary>Loads data from a repository file.</summary>
    /// <param name="xmlFile">The local file path.</param>
    public Repository(string xmlFile)
    {
      XmlTextReader ready = new XmlTextReader(xmlFile);
      m_doc = new XmlDocument();
      m_doc.Load(ready);
      ready.Close();
    }

    /// <summary>Creates a new repository file.</summary>
    /// <param name="xmlFile">The local file path.</param>
    /// <param name="maintainer">The name of the repository maintainer.</param>
    /// <param name="supportUrl">URL for support of this repository.</param>
    /// <returns>A reference to the newly created Repository.</returns>
    public static Repository Create(string xmlFile, string maintainer, string supportUrl)
    {
      string formatstring = "<?xml version=\"1.0\" encoding=\"utf-8\" ?>\r\n<Repository xmlns=\"http://www.acm.uiuc.edu/sigwin/wipt/2006/06\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:SchemaLocation=\"http://www.acm.uiuc.edu/sigwin/wipt/2006/06 http://www.acm.uiuc.edu/sigwin/wipt/2006/06/repository.xsd\" Maintainer=\"{0}\" SupportURL=\"{1}\"></Repository>";

      XmlDocument newDoc = new XmlDocument();
      newDoc.LoadXml(string.Format(formatstring, maintainer, supportUrl));

      XmlTextWriter tex = new XmlTextWriter(xmlFile, Encoding.UTF8);
      tex.Formatting = Formatting.Indented;
      tex.Indentation = 2;
      tex.IndentChar = ' ';
      newDoc.WriteTo(tex);
      tex.Close();

      return new Repository(xmlFile);
    }

    /// <summary>Saves the repository state to a file.</summary>
    /// <param name="xmlFile">The local file path.</param>
    public void Save(string xmlFile)
    {
      XmlTextWriter tex = new XmlTextWriter(xmlFile, Encoding.UTF8);
      tex.Formatting = Formatting.Indented;
      tex.Indentation = 2;
      tex.IndentChar = ' ';

      m_doc.WriteTo(tex);
      tex.Close();
    }

    /// <summary>Adds an MSI package to the repository, creating the product entry if necessary.</summary>
    /// <param name="productName">The name of the product.</param>
    /// <param name="upgradeCode">The product's UpgradeCode.</param>
    /// <param name="publisher">The product's publisher.</param>
    /// <param name="supportUrl">URL for support of the product.</param>
    /// <param name="version">A dotted version string for this package.</param>
    /// <param name="productCode">The package's product code.</param>
    /// <param name="Url">The URL for the package.</param>
    /// <param name="makestable">Whether this package should be made the stable version for the product</param>
    /// <returns>true if the package could be added, false otherwise.</returns>
    public bool AddPackage(string productName, Guid upgradeCode, string publisher, string supportUrl, string version,
        Guid productCode, string Url, bool makestable)
    {
      string formatstring = "<Package xmlns=\"http://www.acm.uiuc.edu/sigwin/wipt/2006/06\" ProductCode=\"{0}\">\r\n      <Version Major=\"{1}\" Minor=\"{2}\" Build=\"{3}\" />\r\n      <URL>{4}</URL>\r\n    </Package>";
      XmlDocumentFragment frag = m_doc.CreateDocumentFragment();
      string[] vp = version.Split('.');
      string[] parts = new string[3]{"0","0","0"};
      Array.Copy(vp, parts, Math.Min(vp.Length, 3));
      frag.InnerXml = string.Format(formatstring, "{" + productCode.ToString().ToUpper() + "}", parts[0], parts[1], parts[2], Url);

      string formatstring2 = "<Product xmlns=\"http://www.acm.uiuc.edu/sigwin/wipt/2006/06\" Name=\"{0}\" UpgradeCode=\"{1}\" Publisher=\"{2}\" SupportURL=\"{3}\" ><StableVersion Major=\"{4}\" Minor=\"{5}\" Build=\"{6}\" /></Product>";
      XmlDocumentFragment frag2 = m_doc.CreateDocumentFragment();
      frag2.InnerXml = string.Format(formatstring2, productName, "{" + upgradeCode.ToString().ToUpper() + "}", publisher, supportUrl, parts[0], parts[1], parts[2]);

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
                  foreach(XmlElement g in t.ChildNodes)
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
