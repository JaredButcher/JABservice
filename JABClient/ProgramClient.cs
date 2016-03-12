using System;
using System.Configuration;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Client
{
    /*JAB Client
        Created by Jared Butcher
        2/3/16
        Version 1.0
        For the purpous of learning how to use sockets and communciate between applicaitons, feedback apreciated
        All rights not reserved becasue that is too much work and why would someone steal this anyway.*/
    class Program
    {

        //Creates Client's socket
        private static Socket Clientsock = new Socket
            (AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        //Creates main buffer
        private static byte[] mainBuff = new byte[1024];
        private static string IPDef = ConfigurationManager.AppSettings.Get("Ip");
        private static string DefName = ConfigurationManager.AppSettings.Get("DefName");
        private static int port = int.Parse(ConfigurationManager.AppSettings.Get("port"));
        private static bool readySend = true;
        private static string soundloca = "Note.wav";
        private static System.Media.SoundPlayer sound = new System.Media.SoundPlayer(soundloca);
        static void Main(string[] args)
        {
            initilization();
        }

        //Welcome message and get IP
        private static void initilization()
        {
            Console.Title = "JAB Client";
            Console.WriteLine("Welcome to the JAB client");
            NeverSurender(IPDef);
            try
            {
                ReceiveData();
                sendMess("name " + DefName);
                sendMess("motd");
                userSendMess();
            }
            catch (SocketException)
            {
                Recon();
            }
            

        }

        //Loops connection untill connected, also allows IP to be changed
        private static void NeverSurender(string ipadd)
        {
            int trys = 0;
            while (!Clientsock.Connected && trys <= 5)
            {
                try
                {
                    trys++;
                    Clientsock.Connect(ipadd, port);
                }
                catch (SocketException)
                {
                    Console.Clear();
                    Console.WriteLine("Connecting to ip: " + ipadd);
                    Console.WriteLine("Connection Atempts: " + trys + " out of 5");
                }
            }

            if (Clientsock.Connected)
            {
                Console.Clear();
                Console.WriteLine("Connected!");
            }
            else
            {
                Console.Clear();
                Console.WriteLine("Did not connect in 5 trys");
                Console.WriteLine("Do you want to try the ip " + ipadd + " again?");
                Console.Write("Y/N/Exit >");
                string resp = Console.ReadLine();
                if (resp.ToLower() == "n")
                {
                    Console.Clear();
                    Console.WriteLine("Enter new IP:");
                    Console.Write("> ");
                    resp = Console.ReadLine();
                    NeverSurender(resp);
                    
                }
                else if(resp.ToLower() == "exit")
                {
                    System.Environment.Exit(0);
                }
                else
                {
                    Console.Clear();
                    NeverSurender(ipadd);
                }
            }
        }

        //user Sends message
        private static void userSendMess()
        {
            string things = Console.ReadLine();
            if (things.ToLower() == "exit")
            {
                System.Environment.Exit(0);
            }
            else if(things.ToLower() == "help")
            {
                //Client Commands
                Console.WriteLine("Exit -Exits Client");
            }
            sendMess(things);
            userSendMess();

        }

        //Sends message
        private static void sendMess(string mess)
        {
            if(readySend == true)
            {
                readySend = false;
                byte[] stuff = Encoding.ASCII.GetBytes(mess);
                Clientsock.BeginSend(stuff, 0, stuff.Length, SocketFlags.None, new AsyncCallback(SendStuff), Clientsock);
            }
            else
            {
                Thread.Sleep(100);
                sendMess(mess);
            }
            
        }

        //Does stuff for Sends message
        private static void SendStuff(IAsyncResult AsRe)
        {

            Socket sock = (Socket)AsRe.AsyncState;
            sock.EndSend(AsRe);
            readySend = true;
        }

        //Receive data
        private static void ReceiveData()
        {
            //Start receiveing data from socket and save it to the main buffer
            Clientsock.BeginReceive(mainBuff, 0, mainBuff.Length, SocketFlags.None, new AsyncCallback(TakeCallBack), Clientsock);
        }

        //Process received data
        private static void TakeCallBack(IAsyncResult AsRe)
        {
            try
            {
                //Save data as int for length purpouses
                int received = Clientsock.EndReceive(AsRe);
                //Creates a buffer of proper length
                byte[] tempBuff = new byte[received];
                //Copys data to new buffer to eliminate null bytes
                Array.Copy(mainBuff, tempBuff, received);
                //Converts message to ASCII
                string message = Encoding.ASCII.GetString(tempBuff);
                //Displays message
                Console.WriteLine("Received: " + message);
                    sound.Play();
                ReceiveData();
            }
            catch(SocketException)
            {
                Recon();
            }
        }
       
        //Reconnect client
        private static void Recon()
        {
            Console.WriteLine("Lost connection to server");
            Clientsock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            initilization();

        }
    }
}
