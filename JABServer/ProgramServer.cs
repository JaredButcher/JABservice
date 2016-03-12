using System;
using System.Configuration;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace Server
{
    /*JAB Server
        Created by Jared Butcher
        2/3/16
        Version 1.0
        For the purpous of learning how to use sockets and communciate between applicaitons, feedback apreciated
        All rights not reserved becasue that is too much work and why would someone steal this anyway.*/
    //Class to save connection infomation to
    public class Connection
    {
        public Socket CSocket { get; set; }
        public int NumID { get; set; }
        public string Name { get; set; }

        public Connection(Socket Socket, int ID, string Nom)
        {
            CSocket = Socket;
            NumID = ID;
            Name = Nom;
            Console.WriteLine("Client ID: " + NumID + " Connected");
        }
        public void close()
        {
            CSocket = null;
            Name = string.Empty;
        }
    }
    class Program
    {
        //Defiens the buffer
        private static byte[] mainBuff = new byte[1024];
        //Saves list of sockets
        private static List<Connection> Connections = new List<Connection>();
        //Creates the server socket
        private static Socket serverSock = new Socket
            (AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private static int idnum = 1;
        private static string SerName = ConfigurationManager.AppSettings.Get("SerName");
        private static int Port = Int32.Parse(ConfigurationManager.AppSettings.Get("Port"));
        private static string motd = ConfigurationManager.AppSettings.Get("motd");

        static void Main(string[] args)
        {
            Console.Title = "JAB Server";
            MakeServer();
            Admin();
        }

        //Adminstravie commands
		//Lenght checks are there to keep program form crashing when useing substrings, if there is a better way then please tell me.
        private static void Admin()
        {
            string command = Console.ReadLine();
            string Lcomm = command.ToLower();
            if (command.Length >= 4)
            {
                if (Lcomm == "exit")
                {
                    Environment.Exit(0);
                }
                if (Lcomm == "help")
                {
                    Console.WriteLine("\r\nExit -Shutsdown server\r\ngmotd -Shows current message of the day\r\nusers -List all currently connected users\r\nsm <user> <message> -Sends a message to a user\r\nsay <message> -Broadcast a message to all users\r\nKick <user> -Kicks a client from the server");
                }
                else if (Lcomm.Substring(0, 3) == "sm ")
                {
                    try
                    {
                        int LenName = command.IndexOf(" ", 3) - 3;
                        string reName = command.Substring(3, LenName);
                        Connection RecSock = Connections.Find(x => x.Name == reName);
                        string mess = SerName + ": " + command.Substring(LenName + 3);
                        SSendStuff(mess, RecSock.CSocket);
                        Console.WriteLine("Message sent");
                    }
                    catch (SystemException)
                    {
                        Console.WriteLine("Error, message not sent");
                    }
                }
                else if (command.Length >= 5)
                {
                    if (Lcomm == "gmotd")
                    {
                        Console.WriteLine(motd);
                    }
                    else if (Lcomm.Substring(0, 4) == "say ")
                    {
                        try
                        {
                            string mess = SerName + ": " + command.Substring(4);
                            foreach (Connection con in Connections)
                            {
                                SSendStuff(mess, con.CSocket);
                            }
                            Console.WriteLine("Message broadcast");
                        }
                        catch (SystemException)
                        {
                            Console.WriteLine("Error, message not sent");
                        }
                    }
                    else if(Lcomm == "users")
                    {
                        foreach(Connection con in Connections)
                        {
                            if (con.Name != string.Empty)
                            {
                                Console.WriteLine(con.Name);
                            }
                        }
                    }
                    else if (command.Length >= 6)
                    {

                        if (Lcomm.Substring(0, 5) == "kick ")
                        {
                            Connection CurCon = Connections.Find(x => x.Name == command.Substring(5));
                            try
                            {
                                CurCon.CSocket.Close();
                                Console.WriteLine("Client has been kicked");
                            }
                            catch (System.NullReferenceException)
                            {
                                Console.WriteLine("Client doesn't exist");
                            }

                        }
                        else if (Lcomm.Substring(0,5) == "motd ")
                        {
                            motd = command.Substring(5);
                            Console.WriteLine("Message of the Day changed");
                        }
                    }
                }
            }
            Admin();
        }

        //Creates the server, Starts accepting request
        private static void MakeServer()
        {
            Console.WriteLine("Waiting for Clients...");
            serverSock.Bind(new IPEndPoint(IPAddress.Any, Port));
            serverSock.Listen(5);
            serverSock.BeginAccept(new AsyncCallback(YesCallBack), null);
        }

        //Accepts connection request and receives data
        private static void YesCallBack(IAsyncResult AsRe)
        {
            //Create and save the soceket
            Socket socket = serverSock.EndAccept(AsRe);
            Connections.Add(new Connection(socket, idnum, string.Empty));
            idnum++;
            //Start receiveing data from socket and save it to the main buffer
            socket.BeginReceive(mainBuff, 0, mainBuff.Length, SocketFlags.None, new AsyncCallback(TakeCallBack), socket);
            //Continues connection loop for multible clients
            serverSock.BeginAccept(new AsyncCallback(YesCallBack), null);
        }

        //Process data from socket, holds all possable client commands
        private static void TakeCallBack(IAsyncResult AsRe)
        {
            Socket sock = (Socket)AsRe.AsyncState;
            Connection CurCon = Connections.Find(x => x.CSocket == sock);

            try
            {
                //Saves data as int for determining length
                int Recd = sock.EndReceive(AsRe);
                //Creates a buffer of the approparate length
                byte[] tempBuff = new byte[Recd];
                //Copies data held on the main buffer to the temp buffer and eliminates any null bytes
                Array.Copy(mainBuff, tempBuff, Recd);
                //Converts data on temp buffer to ASCII chariters
                string ReText = Encoding.ASCII.GetString(tempBuff);
                Console.WriteLine(CurCon.Name + ": " + ReText);

                //Checks if message ask for the time
                string respond = string.Empty;
                string LReText = ReText.ToLower();
                int ReTeL = ReText.Length;
                //Connection CurCon = Connections.Find(x => x.CSocket == sock);

                //Shows client's ID
                if (LReText == "gid")
                {
                    respond = "You are ID: " + CurCon.NumID.ToString();
                }
                else if (ReText.Length >= 4)
                {
                    //Displays list of commands
                    if (LReText == "help")
                    {
                        respond = "\r\nsay <message> - Boradcast a message to all users\r\nusers -list currently connected users\r\nmotd -Displays the Message of the Day\r\ngname -gives current name\r\ngid -gives id number\r\ngtime -gives current time\r\nname <new name> -changes name\r\nsm <receiver name> (message) -sends a message\r\necho <message> -Echos message";
                    }
					//display message of the day
                    else if (LReText == "motd")
                    {
                        respond = motd;
                    }
                    //Sends message to another client
                    else if (LReText.Substring(0, 3) == "sm ")
                    {
                        try
                        {
                            int LenName = ReText.IndexOf(" ", 3) - 3;
                            string reName = ReText.Substring(3, LenName);
                            string from = CurCon.Name;
                            if (CurCon.Name == string.Empty)
                            {
                                from = "ID: " + CurCon.NumID;
                            }
                            string mess = "From: " + from + ": " + ReText.Substring(LenName + 3);
                            Connection RecSock = Connections.Find(x => x.Name == reName);
                            SSendStuff(mess, RecSock.CSocket);
                            respond = "Message sent to: " + RecSock.Name;
                        }
                        catch (SystemException)
                        {
                            respond = "Error, message not sent";
                        }

                    }
                    //Shows client its name
                    else if (LReText == "gname")
                    {
                        respond = "You are: " + CurCon.Name;
                    }
                    else if (ReText.Length >= 5)
                    {
                    //Broadcast message
                    if (LReText.Substring(0, 4) == "say ")
                        {
                            try
                            {
                                string mess = CurCon.Name + ": " + ReText.Substring(4);
                                foreach (Connection con in Connections)
                                {
                                    if (con.CSocket != null && con.CSocket != sock)
                                    {
                                        SSendStuff(mess, con.CSocket);
                                    }
                                }
                                respond = "Message broadcasted";
                            }
                            catch (SystemException)
                            {
                                Console.WriteLine("Error, message not sent");
                            }
                        }
                        //List users
                        else if (LReText == "users")
                        {
                            foreach (Connection con in Connections)
                            {
                                if (con.Name != string.Empty)
                                {
                                    respond = respond + con.Name + "\r\n";
                                }
                            }
                        }
                        //Echos message
                        else if (LReText.Substring(0, 5) == "echo ")
                        {
                            respond = ReText.Substring(5);
                        }
                        else if (LReText == "gtime")
                        {
                            respond = DateTime.Now.ToLongTimeString();
                        }
                        //Changes client's name
                        else if (LReText.Substring(0, 5) == "name ")
                        {
                            string proName = ReText.Substring(5);
                            bool changeName = true;
                            bool noSpace = !proName.Contains(" ");
                            //Checks if name already taken
                            foreach (Connection element in Connections)
                            {
                                if (element.Name == proName)
                                {
                                    changeName = false;
                                }
                            }
                            if (proName == SerName)
                            {
                                changeName = false;
                            }
                            if (changeName && noSpace)
                            {
                                CurCon.Name = proName;
                                respond = "Your name is: " + CurCon.Name;
                            }
                            else if (changeName && !noSpace)
                            {
                                respond = "Name cannot contain spaces";
                            }
                            else if (!changeName && noSpace)
                            {
                                respond = "Name already taken";
                            }
                            else
                            {
                                respond = "This shouldn't be possable, what did you break?";
                            }
                        }
                        else
                        {
                            respond = "Server received message, no farther action taken";
                        }
                    }
                }
                else
                {
                    respond = "Server received message, no farther action taken";
                }
                SSendStuff(respond, sock);
            }
            //If client disconnects
            catch(SocketException)
            {
                Console.WriteLine("Client ID: " + CurCon.NumID + " Disconnected");
                CurCon.close();
            }
            catch (System.ObjectDisposedException)
            {
            }
            catch (SystemException)
            {
                Console.WriteLine("There was an error");
            }
            
        }

        //Sends data
        private static void SendStuff(IAsyncResult AsRe)
        {
            Socket sock = (Socket)AsRe.AsyncState;
            sock.EndSend(AsRe);
        }

        //Starts sending data
        private static void SSendStuff(string respond, Socket sock)
        {
            //Converts the server's response to bytes
            byte[] stuff = Encoding.ASCII.GetBytes(respond);
            //Sends response
            sock.BeginSend(stuff, 0, stuff.Length, SocketFlags.None, new AsyncCallback(SendStuff), sock);
            //Starts receiveing again
            sock.BeginReceive(mainBuff, 0, mainBuff.Length, SocketFlags.None, new AsyncCallback(TakeCallBack), sock);
            //Starts accepting new connections
            serverSock.BeginAccept(new AsyncCallback(YesCallBack), null);
        }
    }
}
