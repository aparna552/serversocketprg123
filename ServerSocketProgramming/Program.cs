using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Security.Cryptography;
using System.Threading;

class Server
{
    static Dictionary<string, Dictionary<string, int>> collection = new Dictionary<string, Dictionary<string, int>>()
    {
        { "SetA", new Dictionary<string, int>{{ "One", 1 }, { "Two", 2 }} },
        { "SetB", new Dictionary<string, int>{{ "Three", 3 }, { "Four", 4 }} },
        { "SetC", new Dictionary<string, int>{{ "Five", 5 }, { "Six", 6 }} },
        { "SetD", new Dictionary<string, int>{{ "Seven", 7 }, { "Eight", 8 }} },
        { "SetE", new Dictionary<string, int>{{ "Nine", 9 }, { "Ten", 10 }} }
    };

    static Aes aes = Aes.Create();
    static readonly byte[] Key = Encoding.UTF8.GetBytes("Your16CharKey123"); 
    static readonly byte[] IV = Encoding.UTF8.GetBytes("Your16CharIV_456");

    static void Main()
    {
        aes.Key = Key;
        aes.IV = IV;

        Console.WriteLine("Server started..");
        TcpListener listener = new TcpListener(IPAddress.Any, 12345);
        listener.Start();

        while (true)
        {
            TcpClient client = listener.AcceptTcpClient();
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[1024];
            int bytesRead = stream.Read(buffer, 0, buffer.Length);
            string encryptedMessage = Encoding.UTF8.GetString(buffer, 0, bytesRead);

            
            Console.WriteLine("Received encrypted text:"+ encryptedMessage);
            Console.WriteLine("Encrypted text length: "+encryptedMessage.Length);
            if (!encryptedMessage.Contains(":"))
            {
                Console.WriteLine("Error: Encrypted text does not contain a colon (:).");
                client.Close();
                continue; 
            }

            try
            {
                string decryptedMessage = Decrypt(encryptedMessage);
                Console.WriteLine("Received:" +decryptedMessage);

                string[] parts = decryptedMessage.Split('-');
                string response;

                if (parts.Length == 2 && collection.ContainsKey(parts[0]) && collection[parts[0]].ContainsKey(parts[1]))
                {
                    int value = collection[parts[0]][parts[1]];

                    
                    for (int i = 0; i < 2; i++) 
                    {
                        string timeMessage = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                        response = Encrypt(timeMessage);
                        byte[] responseBytes = Encoding.UTF8.GetBytes(response);
                        stream.Write(responseBytes, 0, responseBytes.Length);
                        Console.WriteLine("Sent:"+ timeMessage);

                        if (i < 1) 
                        {
                            Thread.Sleep(1000);
                        }
                    }
                }
                else
                {
                    response = Encrypt("EMPTY");
                    byte[] responseBytes = Encoding.UTF8.GetBytes(response);
                    stream.Write(responseBytes, 0, responseBytes.Length);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error:"+ ex.Message);
            }

            client.Close();
        }
    }

    static string Encrypt(string plainText)
    {
        using (ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
        {
            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
            byte[] encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
            return Convert.ToBase64String(encryptedBytes) + ":" + Convert.ToBase64String(aes.IV);
        }
    }

    static string Decrypt(string encryptedText)
    {
        try
        {
            
            string[] parts = encryptedText.Split(':');
            if (parts.Length != 2)
                throw new ArgumentException("Invalid encrypted text format Expected format: '<ciphertext>:<IV>'");

            
            byte[] encryptedBytes = Convert.FromBase64String(parts[0]);
            byte[] iv = Convert.FromBase64String(parts[1]);

           
            using (ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, iv))
            {
                byte[] plainBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
                return Encoding.UTF8.GetString(plainBytes);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error in Decrypt: "+ex.Message);
            throw;
        }
    }
}
