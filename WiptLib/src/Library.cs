/*
 *  Copyright (c) 2006 Association for Computing Machinery at the 
 *  University of Illinois at Urbana-Champaign.
 *  All rights reserved.
 * 
 *  Developed by: Special Interest Group for Windows Development
 *                ACM@UIUC
 *                http://www.acm.uiuc.edu/sigwin
 *
 *  Permission is hereby granted, free of intge, to any person obtaining a 
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
    public class Package : IComparable
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

      /// <summary>
      /// IComparable.CompareTo implementation.
      /// </summary>
      public int CompareTo(object obj)
      {
        if(obj is Package)
        {
          Package p = (Package) obj;
          return version.CompareTo(p.version);
        }

        throw new ArgumentException("object is not a Package");
      }
    }

  /// <remarks>A Transform represents an MST file.</remarks>
  [Serializable()]
    public class Transform
    {
      /// <summary>
      /// A Version object containing the minimum version this transform applies to.
      /// The build member may be null.
      /// </summary>
      public Version minVersion;
      /// <summary>
      /// A Version object containing the maximum version this transform applies to.
      /// The build member may be null.
      /// </summary>
      public Version maxVersion;
      /// <summary>The URL of the transform.</summary>
      public string URL;
      /// <summary>The constructor for the Transform class.</summary>
      public Transform() {}
    }

  /// <remarks>
  /// The Patch class represents a patch.
  /// </remarks>
  [Serializable()]
    public class Patch
    {
      /// <summary>The PatchCode.</summary>
      public Guid patchCode;
      /// <summary>The name of the patch.</summary>
      public string name;
      /// <summary>The product codes to which this patch applies.</summary>
      public Guid[] productCodes;
      /// <summary>The URL of the patch.</summary>
      public string URL;

      /// <summary>The constructor for the Patch class.</summary>
      /// <param name="PatchCode">The PatchCode.</param>
      /// <param name="Name">The name of the patch.</param>
      public Patch(Guid PatchCode, string Name)
      {
        patchCode = PatchCode;
        name = Name;
        productCodes = new Guid[0];
      }
    }

  /// <remarks>
  /// The Product class represents an installable product, such as rwho or
  /// Wipt.
  /// </remarks>
  [Serializable()]
    public class Product : IComparable
    {
      /// <summary>The product's UpgradeCode.</summary>
      public Guid upgradeCode;
      /// <summary>The name of the product.</summary>
      public string name;
      /// <summary>The publisher of the product.</summary>
      public string publisher;
      /// <summary>The support URL for the product.</summary>
      public string supportURL;
      /// <summary>A description of the product.</summary>
      public string description;
      /// <summary>A Version object for the product's release version.</summary>
      public Version stableVersion;
      /// <summary>A array of packages for this product.</summary>
      public Package[] packages;
      /// <summary>An array of transforms for this product.</summary>
      public Transform[] transforms;
      /// <summary>Patches for the product.</summary>
      public Patch[] patches;

      /// <summary>The constructor for the Product class.</summary>
      /// <param name="Name">The name of the Product.</param>
      /// <param name="Publisher">The publisher of the product.</param>
      /// <param name="SupportURL">A support URL for the product.</param>
      public Product(string Name, string Publisher, string SupportURL)
      {
        name = Name;
        publisher = Publisher;
        supportURL = SupportURL;
        packages = new Package[0];
        transforms = new Transform[0];
        patches = new Patch[0];
      }

      /// <summary>
      /// IComparable.CompareTo implementation.
      /// </summary>
      public int CompareTo(object obj)
      {
        if(obj is Product)
        {
          Product p = (Product) obj;
          return name.CompareTo(p.name);
        }

        throw new ArgumentException("object is not a Package");
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
      Stream i;

      try
      {
        i = req.GetResponse().GetResponseStream();

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

                if(library.ContainsKey(curNode.GetAttribute("Name").ToLower()))
                {
                  p = (Product)library[curNode.GetAttribute("Name").ToLower()];
                  if(p.upgradeCode != 
                      new Guid(curNode.GetAttribute("UpgradeCode")))
                  {
                    throw new WiptException("Product name collision on "
                        + p.name);
                  }
                }
                else
                {
                  p = new Product(curNode.GetAttribute("Name"), curNode.GetAttribute("Publisher"), curNode.GetAttribute("SupportURL"));
                  p.upgradeCode = new Guid(curNode.GetAttribute("UpgradeCode"));
                  library.Add(curNode.GetAttribute("Name").ToLower(), p);
                }

                foreach(XmlElement e in curNode.ChildNodes)
                {
                  switch(e.Name)
                  {
                    case "StableVersion":
                      p.stableVersion = new Version(
                          e.GetAttribute("Major") + "." +
                          e.GetAttribute("Minor") + "." +
                          e.GetAttribute("Build")
                          );
                    break;
                    case "Description":
                      p.description = e.InnerText;
                    break;
                    case "Transform":
                      Transform q = new Transform();
                    foreach(XmlElement n in e.ChildNodes)
                    {
                      switch(n.Name)
                      {
                        case "MinVersion":
                          q.minVersion = new Version(
                              n.GetAttribute("Major") + "." +
                              n.GetAttribute("Minor") + "." +
                              n.GetAttribute("Build"));
                        break;
                        case "MaxVersion":
                          q.maxVersion = new Version(
                              n.GetAttribute("Major") + "." +
                              n.GetAttribute("Minor") + "." +
                              n.GetAttribute("Build"));
                        break;
                        case "URL":
                          q.URL = n.InnerText;
                        break;
                      }
                    }

                    Transform[] tmp = new Transform[p.transforms.Length + 1];
                    Array.Copy(p.transforms, tmp, p.transforms.Length);
                    tmp[p.transforms.Length] = q;
                    p.transforms = tmp;
                   
                    break;
                    case "Patch":
                      Patch g = new Patch(new Guid(e.GetAttribute("PatchCode")), e.GetAttribute("Name"));
                    foreach(XmlElement v in e.ChildNodes)
                    {
                      if(v.Name == "URL")
                      {
                        g.URL = v.InnerText;
                      }
                      else if(v.Name == "ProductCode")
                      {
                        Guid[] nm = new Guid[g.productCodes.Length + 1];
                        Array.Copy(g.productCodes, nm, g.productCodes.Length);
                        nm[g.productCodes.Length] = new Guid(v.InnerText);
                        g.productCodes = nm;
                      }
                    }

                    {
                      Patch[] nm = new Patch[p.patches.Length + 1];
                      Array.Copy(p.patches, nm, p.patches.Length);
                      nm[p.patches.Length] = g;
                      p.patches = nm;
                    }

                    break;
                    case "Package":
                      Package a = new Package(e.GetAttribute("ProductCode"));

                      Package[] np = new Package[p.packages.Length + 1];
                      Array.Copy(p.packages, np, p.packages.Length);
                      np[p.packages.Length] = a;
                      p.packages = np;
                      
                      foreach(XmlNode y in e.ChildNodes)
                      {
                        if(y is XmlElement)
                        {
                          XmlElement t = (XmlElement)y;

                          switch(t.Name)
                          {
                            case "Version":
                              a.version = new Version(
                                t.GetAttribute("Major") + "." +
                                t.GetAttribute("Minor") + "." +
                                t.GetAttribute("Build")
                                );
                            break;
                            case "URL":
                              a.URL = t.InnerText;
                            break;
                          } 
                        } 
                      }
                    break;
                  }
                }
              }
            }
          }
        }
      }
      catch(System.Net.WebException)
      {
        return false;
      }

      return true;
    }

    /// <summary>
    /// Retrieve a Product object from the Library.
    /// </summary>
    /// <param name="name">
    /// The name of the product, suite, or patch (case-insensitive).
    /// </param>
    /// <returns>
    /// A Product object or null if not found.
    /// </returns>
    public static Product GetProduct(string name)
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
        return (Product)library[name.ToLower()];
      }

      return null;
    }

    /// <summary>
    /// Returns all of the products in the Library.
    /// </summary>
    /// <returns>
    /// An array of Product objects, or null if an error occurred.
    /// </returns>
    public static Product[] GetAll()
    {
      if(library == null)
      {
        if(!Load())
        {
          throw new WiptException("Could not load package database.");
        }
      }
      Product[] list = new Product[library.Keys.Count];
      int i = 0;
      foreach(object s in library.Keys)
      {
        list[i++] = (Product)library[s];
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
