using System;
using System.Net;
using System.Windows;
using System.Device.Location;
using System.Xml;
using System.Xml.Linq;
using System.Linq;

namespace PostcodeToCoord
{
    public class PostcodeToLongLat
    {
        //Properties
        public delegate void SyncComplete();

        /// <summary>
        /// Event which tells the code when the async has completed
        /// </summary>
        public event SyncComplete xmlComplete;

        private string _bingMapsKey;
        private string _postcode;
        public GeoCoordinate location;
        public double lat, longi;
        private string _apiUri;

        bool success;

        /// <summary>
        /// Create new object of the conversion class
        /// </summary>
        /// <param name="bingMapsApiKey">Your personal Bing Maps API Key</param>
        public PostcodeToLongLat(string bingMapsApiKey)
        {
            if (bingMapsApiKey.Length == 64)
            {
                _bingMapsKey = bingMapsApiKey;
                _apiUri = "http://dev.virtualearth.net/REST/v1/Locations?CountryRegion=UK&output=xml&key=" + _bingMapsKey + "&postalCode=";
                lat = longi = 0.0;
                success = false;
            }
            else
                throw new FormatException("Bing Maps API not supplied.");
        }
        /// <summary>
        /// Set postcode to convert to coordinates
        /// </summary>
        /// <param name="postcode">Full UK Postcode</param>
        public void SetPostcode(string postcode)
        {
            if (postcode.Length == 0)
                throw new FormatException("Cannot accept an empty value.");

            postcode.Replace(" ", string.Empty);

            if (postcode.Length >= 5)
                _postcode = postcode;
            else
                throw new FormatException("Please enter a full UK postcode.");
        }

        /// <summary>
        /// After setting postcode, call this function to fetch the coordinate data
        /// </summary>
        public void GetCoords()
        {
            if (_postcode.Length > 0)
            {
                try
                {
                    WebClient mapSearch = new WebClient();
                    mapSearch.DownloadStringCompleted += new DownloadStringCompletedEventHandler(mapSearch_DownloadStringCompleted);
                    mapSearch.DownloadStringAsync(new Uri(_apiUri + _postcode));
                }
                catch (Exception error)
                {
                    throw error;
                }
            }
            else
            {
                throw new FormatException("Please set postcode before attempting conversion.");
            }
        }

        void mapSearch_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            try
            {
                XDocument xdocns = XDocument.Parse(e.Result);
                XDocument xdoc = RemoveNamespace(xdocns); //This new function remove the namespace data from the xml, makes it easy to work with

                string thisLat, thisLongi;

                //There's probably an easier way to do this than a loop, but since there's only one element, it will work fine.
                foreach (XElement item in xdoc.Elements("Response").Elements("ResourceSets").Elements("ResourceSet").Elements("Resources"))
                {
                    thisLat = item.Element("Location").Element("Point").Element("Latitude").Value;
                    thisLongi = item.Element("Location").Element("Point").Element("Longitude").Value;

                    lat = double.Parse(thisLat);
                    longi = double.Parse(thisLongi);
                }
                location = new GeoCoordinate(lat, longi);
                if ((lat == 0) && (longi == 0))
                    success = false;
                else
                    success = true;
            }
            catch (Exception error)
            {
                success = false;
                throw error;
            }
            finally
            {
                xmlComplete();
            }
        }

        #region XMLNS Remover
        private XDocument RemoveNamespace(XDocument xdoc)
        {
            foreach (XElement e in xdoc.Root.DescendantsAndSelf())
            {
                if (e.Name.Namespace != XNamespace.None)
                {
                    e.Name = XNamespace.None.GetName(e.Name.LocalName);
                }
                if (e.Attributes().Where(a => a.IsNamespaceDeclaration || a.Name.Namespace != XNamespace.None).Any())
                {
                    e.ReplaceAttributes(e.Attributes().Select(a => a.IsNamespaceDeclaration ? null : a.Name.Namespace != XNamespace.None ? new XAttribute(XNamespace.None.GetName(a.Name.LocalName), a.Value) : a));
                }
            }
            return xdoc;
        }
        #endregion
    }
}
