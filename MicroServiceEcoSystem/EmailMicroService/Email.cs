using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmailMicroService
{
    using System.Net;
    using System.Net.Mail;
    using System.Net.Sockets;
    using Microsoft.Win32;

    /// <summary>   An email sender. </summary>
    public class EmailSender
    {
        /// <summary>   Default SMTP Port. </summary>
        public static int SmtpPort = 25;

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Send this message. </summary>
        ///
        /// <param name="from">     Source for the. </param>
        /// <param name="to">       to. </param>
        /// <param name="subject">  The subject. </param>
        /// <param name="body">     The body. </param>
        ///
        /// <returns>   True if it succeeds, false if it fails. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public static bool Send(string from, string to, string subject, string body)
        {
            string domainName = GetDomainName(to);
            IPAddress[] servers = GetMailExchangeServer(domainName);
            foreach (IPAddress server in servers)
            {
                try
                {
                    SmtpClient client = new SmtpClient(server.ToString(), SmtpPort);
                    client.Send(from, to, subject, body);
                    return true;
                }
                catch
                {
                    continue;
                }
            }

            return false;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Send this message. </summary>
        ///
        /// <param name="mailMessage">  Message describing the mail. </param>
        ///
        /// <returns>   True if it succeeds, false if it fails. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public static bool Send(MailMessage mailMessage)
        {
            string domainName = GetDomainName(mailMessage.To[0].Address);
            IPAddress[] servers = GetMailExchangeServer(domainName);
            foreach (IPAddress server in servers)
            {
                try
                {
                    SmtpClient client = new SmtpClient(server.ToString(), SmtpPort);
                    client.Send(mailMessage);
                    return true;
                }
                catch
                {
                    continue;
                }
            }

            return false;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets domain name. </summary>
        ///
        /// <exception cref="ArgumentException">    Thrown when one or more arguments have unsupported or
        ///                                         illegal values. </exception>
        ///
        /// <param name="emailAddress"> The email address. </param>
        ///
        /// <returns>   The domain name. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public static string GetDomainName(string emailAddress)
        {
            int atIndex = emailAddress.IndexOf('@');
            if (atIndex == -1)
            {
                throw new ArgumentException("Not a valid email address", "emailAddress");
            }

            if (emailAddress.IndexOf('<') > -1 && emailAddress.IndexOf('>') > -1)
            {
                return emailAddress.Substring(atIndex + 1, emailAddress.IndexOf('>') - atIndex);
            }
            else
            {
                return emailAddress.Substring(atIndex + 1);
            }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets mail exchange server. </summary>
        ///
        /// <param name="domainName">   Name of the domain. </param>
        ///
        /// <returns>   An array of IP address. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public static IPAddress[] GetMailExchangeServer(string domainName)
        {
            IPHostEntry hostEntry = DomainNameUtil.GetIPHostEntryForMailExchange(domainName);
            if (hostEntry.AddressList.Length > 0)
            {
                return hostEntry.AddressList;
            }
            else if (hostEntry.Aliases.Length > 0)
            {
                return Dns.GetHostAddresses(hostEntry.Aliases[0]);
            }
            else
            {
                return null;
            }
        }
    }

    /// <summary>   A domain name utility. </summary>
    public class DomainNameUtil
    {
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Searches for the first DNS servers. </summary>
        ///
        /// <returns>   The found DNS servers. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public static string[] FindDnsServers()
        {
            RegistryKey start = Registry.LocalMachine;
            string DNSservers = @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters";

            RegistryKey DNSserverKey = start.OpenSubKey(DNSservers);
            if (DNSserverKey == null)
            {
                return null;
            }
            string serverlist = (string)DNSserverKey.GetValue("NameServer");

            DNSserverKey.Close();
            start.Close();
            if (string.IsNullOrWhiteSpace(serverlist))
            {
                return null;
            }
            string[] servers = serverlist.Split(' ');
            return servers;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   some root DNS servers http://en.wikipedia.org/wiki/Root_nameserver. </summary>
        ///
        /// <returns>   The root DNS servers. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        static List<IPAddress> GetRootDnsServers()
        {
            List<IPAddress> rootServers = new List<IPAddress>();
            rootServers.Add(IPAddress.Parse("128.8.10.90"));
            rootServers.Add(IPAddress.Parse("192.168.0.5"));
            rootServers.Add(IPAddress.Parse("192.168.0.1"));

            rootServers.Add(IPAddress.Parse("192.203.230.10"));
            rootServers.Add(IPAddress.Parse("192.36.148.17"));
            rootServers.Add(IPAddress.Parse("192.58.128.30"));
            rootServers.Add(IPAddress.Parse("193.0.14.129"));
            rootServers.Add(IPAddress.Parse("202.12.27.33"));
            return rootServers;
        }
        /// <summary>   The root DNS servers. </summary>
        static List<IPAddress> RootDnsServers = GetRootDnsServers();

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Get IPHostEntry for given domainName. </summary>
        ///
        /// <param name="domainName">   domainName. </param>
        ///
        /// <returns>   The IP host entry. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public static IPHostEntry GetIPHostEntry(string domainName)
        {
            return GetIPHostEntry(domainName, QueryType.Address, FindDnsServers());
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets IP host entry for mail exchange. </summary>
        ///
        /// <param name="domainName">   domainName. </param>
        ///
        /// <returns>   The IP host entry for mail exchange. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public static IPHostEntry GetIPHostEntryForMailExchange(string domainName)
        {
            return GetIPHostEntry(domainName, QueryType.MailExchange, FindDnsServers());
        }

        /// <summary>   The DNS servers. </summary>
        static List<IPAddress> DnsServers = new List<IPAddress>();

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Get IPHostEntry for given domainName. </summary>
        ///
        /// <exception cref="ArgumentException">    Thrown when one or more arguments have unsupported or
        ///                                         illegal values. </exception>
        ///
        /// <param name="domainName">   domainName. </param>
        /// <param name="queryType">    QueryType.Address or QueryType.MailExchange. </param>
        /// <param name="dnsServers">   dnsServers. </param>
        ///
        /// <returns>   The IP host entry. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public static IPHostEntry GetIPHostEntry(string domainName, QueryType queryType, string[] dnsServers)
        {
            if (String.IsNullOrEmpty(domainName))
            {
                throw new ArgumentException("Domain name is empty.", "domainName");
            }
            DnsServers.Clear();
            if (dnsServers != null)
            {
                foreach (string dnsServer in dnsServers)
                {
                    DnsServers.Add(IPAddress.Parse(dnsServer));
                }
            }
            DnsServers.AddRange(RootDnsServers);

            int retry = 0;
            while (retry < 10)
            {
                foreach (IPAddress dnsServer in DnsServers)
                {
                    IPHostEntry ip = GetIPHostEntry(domainName, queryType, dnsServer);
                    if (ip != null)
                    {
                        return ip;
                    }
                }
                retry++;
            }
            return null;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Get IPHostEntry for given domainName. </summary>
        ///
        /// <param name="domainName">   domainName. </param>
        /// <param name="queryType">    QueryType.Address or QueryType.MailExchange. </param>
        /// <param name="dnsServer">    dnsServer. </param>
        ///
        /// <returns>   The IP host entry. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public static IPHostEntry GetIPHostEntry(string domainName, QueryType queryType, IPAddress dnsServer)
        {
            DnsMessage message = DnsMessage.StandardQuery();
            DnsQuery query = new DnsQuery(domainName, queryType);
            message.Querys.Add(query);
            byte[] msgData = DnsMessageCoder.EncodeDnsMessage(message);
            try
            {
                byte[] reply = QueryServer(msgData, dnsServer);
                if (reply != null)
                {
                    DnsMessage answer = DnsMessageCoder.DecodeDnsMessage(reply);
                    if (answer.ID == message.ID)
                    {
                        if (answer.Answers.Count > 0)
                        {
                            IPHostEntry host = new IPHostEntry();
                            host.HostName = domainName;
                            if (queryType == QueryType.Address)
                            {
                                host.AddressList = GetAddressList(domainName, answer);
                            }
                            else if (queryType == QueryType.MailExchange)
                            {
                                host.Aliases = GetMailExchangeAliases(domainName, answer);
                                host.AddressList = GetAddressList(answer, new List<string>(host.Aliases));
                            }
                            return host;
                        }
                        else if (answer.AuthorityRecords.Count > 0)
                        {
                            IPAddress[] serverAddresses = GetAuthorityServers(answer);
                            // depth first search
                            foreach (IPAddress serverIP in serverAddresses)
                            {
                                IPHostEntry host = GetIPHostEntry(domainName, queryType, serverIP);
                                if (host != null)
                                {
                                    return host;
                                }
                            }
                        }
                    }
                }
            }
            catch { }
            return null;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets address list. </summary>
        ///
        /// <param name="domainName">   domainName. </param>
        /// <param name="answer">       The answer. </param>
        ///
        /// <returns>   An array of IP address. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        private static IPAddress[] GetAddressList(string domainName, DnsMessage answer)
        {
            List<IPAddress> addresseList = new List<IPAddress>();
            foreach (DnsResource resource in answer.Answers)
            {
                if (resource.QueryType == QueryType.Address && resource.Name == domainName)
                {
                    IPAddress ipAddress = new IPAddress((byte[])resource.Content);
                    addresseList.Add(ipAddress);
                }
            }
            return addresseList.ToArray();
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets mail exchange aliases. </summary>
        ///
        /// <param name="domainName">   domainName. </param>
        /// <param name="answer">       The answer. </param>
        ///
        /// <returns>   An array of string. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        private static string[] GetMailExchangeAliases(string domainName, DnsMessage answer)
        {
            List<string> aliases = new List<string>();
            foreach (DnsResource resource in answer.Answers)
            {
                if (resource.QueryType == QueryType.MailExchange && resource.Name == domainName)
                {
                    MailExchange mailExchange = (MailExchange)resource.Content;
                    aliases.Add(mailExchange.HostName);
                }
            }
            return aliases.ToArray();
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets authority servers. </summary>
        ///
        /// <param name="answer">   The answer. </param>
        ///
        /// <returns>   An array of IP address. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        private static IPAddress[] GetAuthorityServers(DnsMessage answer)
        {
            List<string> authorities = new List<string>();
            foreach (DnsResource resource in answer.AuthorityRecords)
            {
                if (resource.QueryType == QueryType.NameServer)
                {
                    string nameServer = (string)resource.Content;
                    authorities.Add(nameServer);
                }
            }
            if (answer.AdditionalRecords.Count > 0)
            {
                return GetAddressList(answer, authorities);
            }
            else
            {
                List<IPAddress> serverAddresses = new List<IPAddress>();
                foreach (string authority in authorities)
                {
                    serverAddresses.AddRange(Dns.GetHostAddresses(authority));
                }
                return serverAddresses.ToArray();
            }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets address list. </summary>
        ///
        /// <param name="answer">       The answer. </param>
        /// <param name="authorities">  The authorities. </param>
        ///
        /// <returns>   An array of IP address. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        private static IPAddress[] GetAddressList(DnsMessage answer, List<string> authorities)
        {
            List<IPAddress> serverAddresses = new List<IPAddress>();
            foreach (DnsResource resource in answer.AdditionalRecords)
            {
                if (resource.QueryType == QueryType.Address)
                {
                    if (authorities.Contains(resource.Name))
                    {
                        IPAddress serverIP = new IPAddress((byte[])resource.Content);
                        serverAddresses.Add(serverIP);
                    }
                }
            }
            return serverAddresses.ToArray();
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Queries a server. </summary>
        ///
        /// <param name="query">    The query. </param>
        /// <param name="serverIP"> The server IP. </param>
        ///
        /// <returns>   An array of byte. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        private static byte[] QueryServer(byte[] query, IPAddress serverIP)
        {
            byte[] retVal = null;

            try
            {
                IPEndPoint ipRemoteEndPoint = new IPEndPoint(serverIP, 53);
                Socket udpClient = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

                IPEndPoint ipLocalEndPoint = new IPEndPoint(IPAddress.Any, 0);
                EndPoint localEndPoint = (EndPoint)ipLocalEndPoint;
                udpClient.Bind(localEndPoint);

                udpClient.Connect(ipRemoteEndPoint);

                //send query
                udpClient.Send(query);

                // Wait until we have a reply
                if (udpClient.Poll(5 * 1000000, SelectMode.SelectRead))
                {
                    retVal = new byte[512];
                    udpClient.Receive(retVal);
                }

                udpClient.Close();
            }
            catch
            {
            }

            return retVal;
        }
    }
}
