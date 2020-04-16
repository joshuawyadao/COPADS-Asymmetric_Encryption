/**
 * Joshua Yadao
 * Professor Jeremy Brown
 * CSCI 251
 * 17 April 2020
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Numerics;
using System.Text;
using Newtonsoft.Json;

namespace Messenger
{
    internal static class Given
    {
        internal static BigInteger ModInverse( BigInteger a, BigInteger n )
        {
            BigInteger i = n, v = 0, d = 1;
            while ( a > 0 )
            {
                BigInteger t = i / a, x = a;
                a = i % x;
                i = x;
                x = d;
                d = v - t * x;
                v = x;
            }
            v %= n;
            if ( v < 0 ) v = ( v + n ) % n;
            return v;
        }
    }

    /// <summary>
    /// Class representation of the private.key stored on the disk
    /// </summary>
    internal class PrivateKeys
    {
        /// <summary>
        /// List of emails
        /// </summary>
        public List<string> Emails { get; set; }
        public string Key { get; set; }
    }
    
    internal class Keys
    {
        public string Email { get; set; }
        public string Key { get; set; }  // make sure to be Base64
    }

    internal class Messages
    {
        public string Email { get; set; }
        public string Content { get; set; }  // make sure to be Base64
    }

    internal class Modifier
    {
        private BigInteger DecryptLen( byte[] keyByte, int sIndex )
        {
            var lenByte = new byte[4];
            Array.Copy( keyByte, sIndex, 
                lenByte, 0, 4 );
            if ( BitConverter.IsLittleEndian )
            {
                Array.Reverse( lenByte, 0, lenByte.Length );
            }
            //Console.WriteLine("[{0}]", string.Join(", ", lenByte));
            return new BigInteger( lenByte );
        }

        private BigInteger DecryptContents(BigInteger bLen, byte[] keyByte, int sIndex )
        {
            var byteArr = new byte[ (int) bLen ];
            Array.Copy(keyByte, sIndex, 
                byteArr, 0, (int) bLen );
            if ( !BitConverter.IsLittleEndian )
            {
                Array.Reverse( byteArr, 0, byteArr.Length );
            }
            return new BigInteger( byteArr ); 
        }

        private void EncryptLen( byte[] source, int dIndex, ref byte[] byteArr )
        {
            var lenBytes = new byte[] { 0, 0, 0, 0 };
            var tempE = BitConverter.GetBytes( source.Length );
            Array.Copy(tempE, 0,
                lenBytes, 0, tempE.Length);
            if ( BitConverter.IsLittleEndian )
            {
                Array.Reverse( lenBytes, 0, lenBytes.Length );
            }
            Array.Copy( lenBytes, 0, 
                byteArr, dIndex, lenBytes.Length );
        }

        private void EncryptContents( byte[] source, int dIndex, ref byte[] byteArr)
        {
            var byteContents = new byte[ source.Length ];
            Array.Copy(source, 0, 
                byteContents, 0, source.Length);
            if ( !BitConverter.IsLittleEndian )
            {
                Array.Reverse( byteContents, 0, byteContents.Length );
            }
            Array.Copy( byteContents, 0, 
                byteArr, dIndex, byteContents.Length);
        }
        
        public List<BigInteger> DecryptKey( string base64Key )
        {
            var keyByte = Convert.FromBase64String( base64Key );  // big endian

            var eBigInt = DecryptLen( keyByte, 0 );  // length of e
            var e = DecryptContents(eBigInt, keyByte, 4);  // contents of e
            var nBigInt = DecryptLen(keyByte, 4 + (int) eBigInt);  // length of n
            var n = DecryptContents(nBigInt, keyByte, 8 + (int) eBigInt);  // contents of n
            
            return new List<BigInteger> { e, n };
        }

        public string EncryptKey( BigInteger e , BigInteger n )
        {
            var eByteSource = e.ToByteArray();  // little endian
            var nByteSource = n.ToByteArray();  // little endian

            var byteArr = new byte[ 8 + eByteSource.Length + nByteSource.Length ];

            EncryptLen(eByteSource, 0, ref byteArr);  // length of e
            EncryptContents(eByteSource, 4, ref byteArr);  // contents of e
            EncryptLen(nByteSource, 4 + eByteSource.Length, ref byteArr);  // length of n
            EncryptContents(nByteSource, 8 + eByteSource.Length, ref byteArr);  // contents of n
            
            return Convert.ToBase64String( byteArr );
        }
    }
    internal class ReqTasks
    {
        private readonly Modifier Mod = new Modifier();
        private readonly HttpClient Client = new HttpClient();
        internal void GenerateKey( int keySize )
        {
            var primeGen = new PrimeGen();
            var rand = new Random();
            
            var factor = rand.Next( 5, 10 );
            var firstSize = (keySize / 2) + rand.Next(-keySize / factor, keySize / factor);
            
            var p = primeGen.GenPrimeNum( firstSize, 1 );
            var q = primeGen.GenPrimeNum( keySize - firstSize, 1 );
            
            var n = p * q;
            var r = ( p - 1 ) * ( q - 1 );
            var e = primeGen.GenPrimeNum( 16, 1 );
            var d = Given.ModInverse( e, r ); 

            // below works fine
            var publicKeyStr = Mod.EncryptKey( e, n );
            var privateKeyStr = Mod.EncryptKey( d, n );

            var publicKey = new Keys() { Email = "", Key = publicKeyStr };
            var privateKey = new PrivateKeys() { Emails = new List<string>(), Key = privateKeyStr };

            var publicKeyJson = JsonConvert.SerializeObject( publicKey, Formatting.Indented );
            var privateKeyJson = JsonConvert.SerializeObject( privateKey, Formatting.Indented );

            File.WriteAllText( "public.key", publicKeyJson );
            File.WriteAllText("private.key", privateKeyJson );
            
            Console.WriteLine("e: {0}", e);
            Console.WriteLine("n: {0}", n);
        }
        internal void SendKey( string email )
        {
            try
            {
                var privateKey = JsonConvert.DeserializeObject<PrivateKeys>(File.ReadAllText("private.key" ));
                var publicKey = JsonConvert.DeserializeObject<Keys>( File.ReadAllText( "public.key" ) );
                
                publicKey.Email = email;
                
                var publicKeyJson = JsonConvert.SerializeObject(publicKey, Formatting.Indented);

                var response = Client.PutAsync("http://kayrun.cs.rit.edu:5000/Key/" + email,
                    new StringContent(publicKeyJson, Encoding.UTF8, "application/json")).Result;
                response.EnsureSuccessStatusCode();

                privateKey.Emails.Add(email);
                var privateKeyJson = JsonConvert.SerializeObject(privateKey, Formatting.Indented);
                File.WriteAllText("private.key", privateKeyJson);
                
            }
            catch (FileNotFoundException)
            {
                throw new FileNotFoundException("Cannot find " + email + ".key to send key");
            }
        }
        internal void SendMessage( string email, string plaintext )
        {
            try
            {
                var keyObj = JsonConvert.DeserializeObject<Keys>( File.ReadAllText( email + ".key" ) );
                
                var decodeEn = Mod.DecryptKey( keyObj.Key );
                var e = decodeEn[0];
                var n = decodeEn[1];
                
                Console.WriteLine("E: {0}\nN: {1}", e, n);

                var textByte = Encoding.UTF8.GetBytes(plaintext);
                var bigText = new BigInteger(textByte);
                
                var cypherText = BigInteger.ModPow(bigText, e, n);
                
                var message = new Messages() { Email = email, 
                    Content = Convert.ToBase64String(cypherText.ToByteArray())};
                var messageJson = JsonConvert.SerializeObject(message, Formatting.Indented);

                var response = Client.PutAsync("http://kayrun.cs.rit.edu:5000/Message/" + email,
                        new StringContent(messageJson, Encoding.UTF8, "application/json")).Result;
                response.EnsureSuccessStatusCode();
            }
            catch (FileNotFoundException)
            {
                throw new FileNotFoundException("Cannot find " + email + ".key to send message");
            }
        }

        internal void GetKey( string email )
        {
            var response = Client.GetAsync("http://kayrun.cs.rit.edu:5000/Key/" + email).Result;
            response.EnsureSuccessStatusCode();

            var jsonObj = response.Content.ReadAsStringAsync().Result;
            var keyObj = JsonConvert.DeserializeObject<Keys>(jsonObj);
            
            var newJsonObj = JsonConvert.SerializeObject(keyObj, Formatting.Indented);

            File.WriteAllText(email + ".key", newJsonObj);
        }

        internal void GetMessage( string email )
        {
            var response = Client.GetAsync("http://kayrun.cs.rit.edu:5000/Message/" + email).Result;
            response.EnsureSuccessStatusCode();
            
            var jsonObj = response.Content.ReadAsStringAsync().Result;
            
            var messageObj = JsonConvert.DeserializeObject<Messages>(jsonObj);

            var privateKeyObj = JsonConvert.DeserializeObject<PrivateKeys>(File.ReadAllText("private.key" ));
            
            /*if (!privateKeyObj.Emails.Contains(email))
            {
                throw new ArgumentException(email + " was not found in private.key. " +
                                            "Please send " + email + " a key first" );
            } */
            
            var decodeEn = Mod.DecryptKey( privateKeyObj.Key );
            var d = decodeEn[0];
            var n = decodeEn[1];

            var messageByte = Convert.FromBase64String(messageObj.Content);

            var cypherText = new BigInteger(messageByte);
            var plainText = BigInteger.ModPow(cypherText, d, n);

            var textBytes = plainText.ToByteArray();

            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(textBytes, 0, textBytes.Length);
            }
            
            Console.WriteLine(Encoding.UTF8.GetString(textBytes));
        }
    }
    
    public static class Program
    {
        private static readonly ReqTasks Request = new ReqTasks();
        private enum Options
        {
            KeyGen,
            SendKey,
            GetKey,
            SendMsg,
            GetMsg,
            Invalid
        }
        
        private static void Usage( int variation = 0, Options op = Options.Invalid )
        {
            switch ( variation )
            {
                case 1:
                    Console.WriteLine( "<options>: keyGen, sendKey, getKey, sendMsg, getMsg" );
                    break;
                default:
                    Console.WriteLine( "dotnet run <option> <other arguments>\n" );
                    Console.WriteLine( "{0,15}", "<options>: keyGen, sendKey, getKey, sendMsg, getMsg" );
                    Console.WriteLine( "{0,29}", "<keyGen> <keysize>");
                    Console.WriteLine( "{0,60}", "<keysize>: size of keys generated");
                    break;
            }
            Environment.Exit( 1 );
        }
        
        private static Options GetOptions( string arg )
        {
            var val = arg switch
            {
                "keyGen" => Options.KeyGen,
                "sendKey" => Options.SendKey,
                "getKey" => Options.GetKey,
                "sendMsg" => Options.SendMsg,
                "getMsg" => Options.GetMsg,
                _ => Options.Invalid
            };
            return val;
        }
        
        public static void Main( string[] args )
        {
            if ( args.Length < 2 || args.Length > 3 ) Usage();
            var option = GetOptions( args[0] );
            if ( option == Options.Invalid ) Usage( 1 );

            switch (option)
            {
                case Options.KeyGen:
                    if ( args.Length != 2 ) Usage();
                    Request.GenerateKey( Convert.ToInt32( args[1] ) );
                    break;
                case Options.SendKey:
                    if ( args.Length != 2 ) Usage();
                    Request.SendKey( args[1] );
                    break;
                case Options.GetKey:
                    if ( args.Length != 2 ) Usage();
                    Request.GetKey( args[1] );
                    break;
                case Options.SendMsg:
                    if ( args.Length != 3 ) Usage();
                    Request.SendMessage( args[1], args[2] );
                    break;
                case Options.GetMsg:
                    if ( args.Length != 2 ) Usage();
                    Request.GetMessage( args[1] );
                    break;
                case Options.Invalid:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}