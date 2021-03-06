﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Xml.Linq;

namespace Rebus.Persistence.Xml
{
    /// <summary>
    /// Class for storing Rebus subscriptions in XML
    /// </summary>
    public class XmlSubscriptionStorage : IStoreSubscriptions
    {
        private readonly object fileLock = new object();
        private readonly string xmlFilePath;

        /// <summary>
        /// Creates a new instance of the XmlSubscriptionStorage
        /// </summary>
        /// <param name="xmlFilePath">Full path to target XML document. File can exist with existing subscriptions, but will be created if not found. Process must have write access to target directory.</param>
        public XmlSubscriptionStorage(string xmlFilePath)
        {
            if (string.IsNullOrEmpty(xmlFilePath))
                throw new ArgumentNullException("xmlFilePath");

            var dir = Path.GetDirectoryName(xmlFilePath);
            
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            this.xmlFilePath = xmlFilePath;
        }

        public string[] GetSubscribers(Type messageType)
        {
            if (messageType == null)
                throw new ArgumentNullException("messageType");

            lock (fileLock)
            {
                var doc = GetSubscriptionDocument();
                var subscriptions = GetSubscriptions(doc);
                var key = Key(messageType);
                return subscriptions.Where(s => s.Type == key).Select(s => s.Queue).ToArray();
            }
        }

        public void Remove(Type messageType, string subscriberInputQueue)
        {
            if (messageType == null)
                throw new ArgumentNullException("messageType");
            if (string.IsNullOrEmpty(subscriberInputQueue))
                throw new ArgumentNullException("subscriberInputQueue");

            lock (fileLock)
            {
                var existingDoc = GetSubscriptionDocument();
                var newDoc = CreateSubscriptionDocument();
                var key = Key(messageType);
                var subscriptions = GetSubscriptions(existingDoc);
                var newSubscriptions = from s in subscriptions
                                       where !(s.Type == key && s.Queue == subscriberInputQueue)
                                       select s;
                newDoc.Root.Add(from s in newSubscriptions
                                select CreateSubscription(s.Queue, s.Type)
                                );
                newDoc.Save(xmlFilePath);
            }
        }

        public void Store(Type messageType, string subscriberInputQueue)
        {
            if (messageType == null)
                throw new ArgumentNullException("messageType");
            if (string.IsNullOrEmpty(subscriberInputQueue))
                throw new ArgumentNullException("subscriberInputQueue");

            lock (fileLock)
            {
                XDocument doc = GetSubscriptionDocument();
                var key = Key(messageType);
                var subscriptionExist = GetSubscriptions(doc, key).Any(s => s.Queue == subscriberInputQueue);
                if (subscriptionExist)
                    return;

                doc.Root.Add(
                    CreateSubscription(subscriberInputQueue, key)
                );
                doc.Save(xmlFilePath);
            }
        }

        /// <summary>
        /// Creates an XElement from a subscription set
        /// </summary>
        /// <param name="subscriberInputQueue">Queue name to store</param>
        /// <param name="type">Type to use</param>
        /// <returns>An XElement representing the subscription</returns>
        private static XElement CreateSubscription(string subscriberInputQueue, string type)
        {
            return new XElement("subscription",
                                    new XElement("type", type),
                                    new XElement("subscriptionEntry", subscriberInputQueue)
                                );
        }

        /// <summary>
        /// Loads the subscription document from disk if it exists, otherwise creates a new
        /// </summary>
        /// <returns>An XDocument with current subscriptions</returns>
        private XDocument GetSubscriptionDocument()
        {
            if (File.Exists(xmlFilePath))
                return XDocument.Load(xmlFilePath);

            return CreateSubscriptionDocument();
        }

        /// <summary>
        /// Creates a new (and empty) subscription document
        /// </summary>
        /// <returns>An XDocument with no subscriptions</returns>
        private XDocument CreateSubscriptionDocument()
        {
            var doc = new XDocument();
            var root = new XElement("subscriptions");
            doc.Add(root);
            return doc;
        }

        /// <summary>
        /// Gets a list of subscriptions
        /// </summary>
        /// <param name="doc">XDocument to search for subscriptions in</param>
        /// <param name="type">Optional type to search for</param>
        /// <returns>A list of current subscriptions</returns>
        private IEnumerable<Subscription> GetSubscriptions(XDocument doc, string type = null)
        {
            if (doc == null)
                throw new ArgumentNullException("doc");

            if (!string.IsNullOrEmpty(type))
            {
                return from s in doc.Descendants("subscription")
                       where s.Element("type").Value == type
                       select new Subscription { Type = type, Queue = s.Element("subscriptionEntry").Value };
            }
            else
                return from s in doc.Descendants("subscription")
                       select new Subscription { Type = s.Element("type").Value, Queue = s.Element("subscriptionEntry").Value };
        }

        /// <summary>
        /// Gets the key for a message type
        /// </summary>
        /// <param name="t">Type to get key for</param>
        /// <returns>A key</returns>
        private string Key(Type t)
        {
            return t.AssemblyQualifiedName;
        }

        /// <summary>
        /// Clears all subscriptions
        /// </summary>
        public void ClearAllSubscriptions()
        {
            lock (fileLock)
            {
                if (File.Exists(xmlFilePath))
                    File.Delete(xmlFilePath);
            }
        }

        /// <summary>
        /// Helper class for subscription entries
        /// </summary>
        class Subscription
        {
            /// <summary>
            /// Gets or sets the type
            /// </summary>
            public string Type { get; set; }
            /// <summary>
            /// Gets or sets the queue name
            /// </summary>
            public string Queue { get; set; }
        }
    }
}