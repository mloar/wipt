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
using System.Collections;
using System.IO;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml;
using Microsoft.Win32;

namespace ACM.Wipt
{
  /// <remarks>A Package represents an MSI file.</remarks>
  [Serializable()]
    public class Package
    {
      /// <summary>The MSI's ProductCode.</summary>
      public Guid productCode;
      /// <summary>A Version object containing the MSI ProductVersion.</summary>
      public Version version;
      /// <summary>The URL of the package.</summary>
      public string URL;
      /// <summary>The constructor for the Package class.</summary>
      /// <param name="ProductCode">
      /// The ProductCode of the Package.  Format-agnostic, but must be
      /// valid GUID.
      /// </param>
      public Package(string ProductCode)
      {
        productCode = new Guid(ProductCode);
      }
    }

  /// <remarks>A Transform represents an MST file.</remarks>
  [Serializable()]
    public class Transform
    {
      /// <summary>
      /// A Version object containing the version this transform applies to.
      /// The build member may be null.
      /// </summary>
      public Version version;
      /// <summary>The URL of the transform.</summary>
      public string URL;
      /// <summary>The constructor for the Transform class.</summary>
      public Transform() {}
    }
  
  /// <remarks>
  /// The Version object represents a Major.Minor.Build version string.
  /// </remarks>
  [Serializable()]
    public class Version
    {
      /// <summary>The major version number.</summary>
      public int major;
      /// <summary>The minor version number.</summary>
      public int minor;
      /// <summary>The build version number.</summary>
      public int build;
      /// <summary>The constructor for the Version class.</summary>
      /// <param name="Major">The major version number.</param>
      /// <param name="Minor">The minor version number.</param>
      /// <param name="Build">The build version number.</param>
      public Version(string Major, string Minor, string Build)
      {
        if(Major != "")
          major = int.Parse(Major);
        if(Minor != "")
          minor = int.Parse(Minor);
        if(Build != "")
          build = int.Parse(Build);
      }
        public override string ToString()
        {
            return string.Format("{0}.{1}.{2}",major,minor,build);
        }
      public static bool operator <(Version v1, Version v2)
      {
        return v1.major < v2.major || ((v1.major == v2.major) && v1.minor < v2.major)
            || ((v1.major == v2.major) && (v1.minor == v2.minor) && (v1.build < v2.build));
      }
      public override bool Equals(object o)
      {
        if(!(o is Version))
        {
          return false;
        }
        
        Version v = (Version)o;
        return (major == v.major) && (minor == v.minor) && (build == v.build);
      }
      public static bool operator >(Version v1, Version v2)
      {
        return !((v1 < v2) || (v1 == v2));
      }
    }

  /// <remarks>
  /// The Dependency class represents a dependency.
  /// </remarks>
  [Serializable()]
    public class Dependency
    {
      public string productName;
      public Version minVersion;
      public Version maxVersion;

      public Dependency(string ProductName)
      {
        productName = ProductName;
      }
    }

  /// <remarks>
  /// The Product class represents an installable product, such as rwho or
  /// Wipt.
  /// </remarks>
  [Serializable()]
    public class Product
    {
      /// <summary>The product's UpgradeCode.</summary>
      public Guid upgradeCode;
      /// <summary>The name of the product.</summary>
      public string name;
      /// <summary>A array of packages for this product.</summary>
      public Package[] packages;
      /// <summary>A Version object for the product's release version.</summary>
      public Version stableVersion;
      /// <summary>
      /// A Version object for the product's development version.
      /// </summary> 
      public Version develVersion;
      /// <summary>An array of Dependency objects this product depends on.</summary>
      public Dependency[] dependencies;
      /// <summary>An array of transforms for this product.</summary>
      public Transform[] transforms;

      /// <summary>The constructor for the Product class.</summary>
      /// <param name="Name">The name of the Product.</param>
      public Product(string Name)
      {
        name = Name;
      }
    }

  /// <remarks>
  /// The Suite class represents a group of products that are independent, but
  /// are often installed together.
  /// </remarks>
  [Serializable()]
    public class Suite
    {
      /// <summary>The name of the suite.</summary>
      public string name;
      
      /// <summary>A array of the names of the products in the suite.</summary>
      public string[] products;

      /// <summary>
      /// The constructor for the Suite class.
      /// </summary>
      /// <param name="Name">
      /// The name of the suite.
      /// </param>
      /// <param name="Products">
      /// An array of strings for the products member.
      /// </param>
      public Suite(string Name, string[] Products)
      {
        products = Products;
        name = Name;
      }
    }

  /// <remarks>
  /// The WiptException class defines exceptions for Wipt.
  /// </remarks>
  public class WiptException : System.ApplicationException
  {
    internal WiptException(string message) : base(message) {}
    internal WiptException(string message, Exception innerException) : 
      base(message, innerException) {}
  }


  /// <remarks>
  /// The Library class contains methods to manage the Wipt Library.  The
  /// Library is analogous to the APT cache, and stores cached package
  /// information from repositories.
  /// 
  /// Methods are not guaranteed to be thread safe.
  /// </remarks>
  public class Library
  {
    private static Hashtable library;

    /// <summary>
    /// Used internally by the Library to retrieve package database from disk.
    /// </summary>
    /// <returns>
    /// A boolean value indicating whether the database was loaded from disk.
    /// </returns>
    private static bool Load()
    {
      string path = Environment.GetFolderPath(
          Environment.SpecialFolder.CommonApplicationData) + "\\ACM\\Wipt";
      Directory.CreateDirectory(path);
      path += "\\library.dat";

      FileStream st;
      try
      {
        st = new FileStream(path, FileMode.Open, FileAccess.Read,
            FileShare.Read);
      }
      catch(IOException)
      {
        library = Hashtable.Synchronized(new Hashtable());
        return true;
      }

      try
      {
        BinaryFormatter formatter = new BinaryFormatter();
        library = Hashtable.Synchronized((Hashtable)formatter.Deserialize(st));
      }
      catch(SerializationException)
      {
        st.Close();
        library = Hashtable.Synchronized(new Hashtable());
        return false;
      }

      st.Close();

      return true;
    }

    /// <summary>
    /// Used internally by the Library to save the package database to disk.
    /// </summary>
    /// <returns>
    /// A boolean value indicating whether the database was written to disk.
    /// </returns>
    private static bool Save()
    {
      string path = Environment.GetFolderPath(
          Environment.SpecialFolder.CommonApplicationData) + "\\ACM\\Wipt";
      Directory.CreateDirectory(path);
      path += "\\library.dat";

      FileStream st;
      try
      {
        st = new FileStream(path, FileMode.Create, 
            FileAccess.Write, FileShare.None);
      }
      catch(IOException)
      {
        return false;
      }

      BinaryFormatter formatter = new BinaryFormatter();
      formatter.Serialize(st, library);
      st.Close();

      return true;

    }

    /// <summary>
    /// The Update method checks for new versions of the package lists for all
    /// of the repositories in the sources list.
    /// </summary>
    /// <returns>
    /// A boolean value indicating if update succeeded for all repositories.
    /// </returns>
    /// <exception cref="WiptException">
    /// Throws a WiptException if bad configuration data is encountered.
    /// </exception>
    public static bool Update()
    {
      if(library != null)
        library.Clear();
      else
        library = new Hashtable();

      bool ret = true;

      RegistryKey rk = Registry.LocalMachine.OpenSubKey("SOFTWARE\\ACM\\Wipt");
      RegistryKey rk2 = Registry.CurrentUser.OpenSubKey("SOFTWARE\\ACM\\Wipt");

      if(rk == null && rk2 == null)
      {
        throw new WiptException(
            "Registry key HKLM\\SOFTWARE\\ACM\\Wipt does not exist.");
      }

      object URLs;
      if(rk2 != null)
      {
        URLs = rk2.GetValue("Repositories");
        if(!(URLs is String))
        {
          URLs = rk.GetValue("Repositories");
        }
      }
      else
      {
        URLs = rk.GetValue("Repositories");
      }

      if(!(URLs is String))
      {
        throw new WiptException(
            "Registry value Repositories does not exist or is not a string.");
      }

      string[] repositories = ((string)URLs).Split(' ');

      foreach(string URL in repositories)
      {

        try
        {
          ret = ret && Update(URL);
        }
        catch(UriFormatException e)
        {
          throw new WiptException("Invalid URL: " + URL, e);
        }
        catch(NotSupportedException e)
        {
          throw new WiptException(
              "Repository URL is for an unsupported protocol: " + URL, e);
        }
      }

      Save();

      if(rk2 != null)
        rk2.Close();
      if(rk != null)
        rk.Close();

      return ret;
    }

    /// <summary>
    /// Used internally by the Library to update the package list for a single
    /// repository.
    /// </summary>
    /// <param name="URL">
    /// URL of the package file.
    /// </param>
    /// <returns>
    /// A boolean value indicating whether the update succeeded.
    /// </returns>
    /// <exception cref="WiptException">
    /// An exception is thrown if two product names match, but the upgrade
    /// codes differ.
    /// </exception>
    private static bool Update(string URL)
    {
      WebRequest req = WebRequest.Create(new Uri(URL));

      // TODO: Make it handle other protocols
      HttpWebResponse resp = (HttpWebResponse)req.GetResponse();
      if(resp.StatusCode != HttpStatusCode.OK)
        return false;

      Stream i = resp.GetResponseStream();
      XmlDocument repository = LoadRepository(i);
      i.Close();

      foreach(XmlLinkedNode FirstLevel in repository.ChildNodes)
      {
        if(FirstLevel.Name == "Repository")
        {
          foreach(XmlElement curNode in FirstLevel.ChildNodes)
          {
            if(curNode.Name == "Product")
            {
              Product p;

              if(library.ContainsKey(curNode.GetAttribute("Name")))
              {
                p = (Product)library[curNode.GetAttribute("Name")];
                if(p.upgradeCode != 
                    new Guid(curNode.GetAttribute("UpgradeCode")))
                {
                  throw new WiptException("Product name collision on "
                      + p.name);
                }
              }
              else
              {
                p = new Product(curNode.GetAttribute("Name"));
                p.upgradeCode = new Guid(curNode.GetAttribute("UpgradeCode"));
                library.Add(curNode.GetAttribute("Name").ToLower(), p);
              }

              foreach(XmlElement e in curNode.ChildNodes)
              {
                switch(e.Name)
                {
                  case "Publisher":
                    break;
                  case "StableVersion":
                    p.stableVersion = new Version(
                        e.GetAttribute("Major"),
                        e.GetAttribute("Minor"),
                        e.GetAttribute("Build")
                        );
                    break;
                  case "DevelVersion":
                    p.develVersion = new Version(
                        e.GetAttribute("Major"),
                        e.GetAttribute("Minor"),
                        e.GetAttribute("Build")
                        );
                    break;
                  case "Dependency":
                    Dependency d = new Dependency(e.GetAttribute("ProductName"));
                    if(p.dependencies == null)
                    {
                      p.dependencies = new Dependency[1];
                      p.dependencies[0] = d;
                    }
                    else
                    {
                      Dependency[] nd = new Dependency[p.dependencies.Length + 1];
                      Array.Copy(p.dependencies, nd, p.dependencies.Length);
                      nd[p.dependencies.Length] = d;
                      p.dependencies = nd;
                    }
                    foreach(XmlElement t in e.ChildNodes)
                    {
                      switch(t.Name)
                      {
                        case "MinVersion":
                          d.minVersion = new Version(
                              t.GetAttribute("Major"),
                              t.GetAttribute("Minor"),
                              t.GetAttribute("Build")
                              );
                        break;
                        case "MaxVersion":
                          d.maxVersion = new Version(
                              t.GetAttribute("Major"),
                              t.GetAttribute("Minor"),
                              t.GetAttribute("Build")
                              );
                        break;
                      }
                    }
                    break;
                  case "Package":
                    Package a = new Package(e.GetAttribute("ProductCode"));
                    if(p.packages == null)
                    {
                      p.packages = new Package[1];
                      p.packages[0] = a;
                    }
                    else
                    {
                      Package[] np = new Package[p.packages.Length + 1];
                      Array.Copy(p.packages, np, p.packages.Length);
                      np[p.packages.Length] = a;
                      p.packages = np;
                    }
                    foreach(XmlElement t in e.ChildNodes)
                    {
                      switch(t.Name)
                      {
                        case "Version":
                          a.version = new Version(
                              t.GetAttribute("Major"),
                              t.GetAttribute("Minor"),
                              t.GetAttribute("Build")
                              );
                        break;
                        case "URL":
                          a.URL = t.InnerText;
                        break;
                      }
                    }
                    break;
                  case "Transform":
                    Transform q = new Transform();
                    if(p.transforms == null)
                    {
                      p.transforms = new Transform[1];
                      p.transforms[0] = q;
                    }
                    else
                    {
                      Transform[] nm = new Transform[p.transforms.Length + 1];
                      Array.Copy(p.transforms, nm, p.transforms.Length);
                      nm[p.transforms.Length] = q;
                      p.transforms = nm;
                    }
                    foreach(XmlElement t in e.ChildNodes)
                    {
                      switch(t.Name)
                      {
                        case "Version":
                          q.version = new Version(
                            t.GetAttribute("Major"),
                            t.GetAttribute("Minor"),
                            t.GetAttribute("Build")
                            );
                        break;
                        case "URL":
                          q.URL = t.InnerText;
                        break;
                      }
                    }
                    break;
                }
              }
            }
            else if(curNode.Name == "Suite")
            {
              string[] array = new string[curNode.ChildNodes.Count];
              int j = 0;
              foreach(XmlElement product in curNode.ChildNodes)
              {
                array[j++] = product.InnerText;
              }

              library.Add(curNode.GetAttribute("Name").ToLower(), new Suite(curNode.GetAttribute("Name"),array));
            }
          }
        }
      }

      return true;
    }

    /// <summary>
    /// Retrieve a Product or suite object from the Library.
    /// </summary>
    /// <param name="name">
    /// The name of the product or suite (case-insensitive).
    /// </param>
    /// <returns>
    /// A Product or Suite object or null if not found.
    /// </returns>
    public static object GetProduct(string name)
    {
      if(library == null)
      {
        if(!Load())
        {
          throw new WiptException("Could not load package database.");
        }
      }
      if(library.ContainsKey(name.ToLower()))
      {
        return library[name.ToLower()];
      }

      return null;
    }

    /// <summary>
    /// Returns all of the products and suites in the Library.
    /// </summary>
    /// <returns>
    /// An array of Product and Suite objects, or null if an error occurred.
    /// </returns>
    public static object[] GetAll()
    {
      if(library == null)
      {
        if(!Load())
        {
          throw new WiptException("Could not load package database.");
        }
      }
      object[] list = new object[library.Keys.Count];
      int i = 0;
      foreach(object s in library.Keys)
      {
        list[i++] = library[s];
      }

      return list;
    }

    private static XmlDocument LoadRepository(Stream st)
    {
      XmlValidatingReader VReader = 
        new XmlValidatingReader(new XmlTextReader(st));

      VReader.ValidationType = ValidationType.Schema;
      VReader.Schemas.Add("urn:xmlns:sigwin:wipt-get:repository",
          "http://www.acm.uiuc.edu/sigwin/WiptSchema.xsd");
      VReader.ValidationEventHandler += 
        new System.Xml.Schema.ValidationEventHandler(
            ValidationEventHandler);

      XmlDocument myRepository = new XmlDocument();
      myRepository.Load(VReader);
      return myRepository;
    }

    private static void ValidationEventHandler(object sender,
        System.Xml.Schema.ValidationEventArgs e)
    {
      throw new WiptException("The packages file failed to validate.", 
          e.Exception);
    }
  }
}
