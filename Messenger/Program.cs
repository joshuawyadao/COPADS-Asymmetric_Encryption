/**
 * Joshua Yadao
 * Professor Jeremy Brown
 * CSCI 251
 * 17 April 2020
 */

/** 
 * TODO: Check is size of message > something
 * TODO: update usage for user to clearly give help
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
        /// List of all emails where can receive messages
        /// </summary>
        public List<string> Emails { get; set; }
        
        /// <summary>
        /// private key
        /// </summary>
        public string Key { get; set; }
    }
    
    /// <summary>
    /// Class representation of general json key structure
    /// </summary>
    internal class Keys
    {
        /// <summary>
        /// Email of key location
        /// </summary>
        public string Email { get; set; }
        
        /// <summary>
        /// Base64 encoded key 
        /// </summary>
        public string Key { get; set; }
    }

    /// <summary>
    /// Class representation of the general json message structure
    /// </summary>
    internal class Messages
    {
        /// <summary>
        /// Email location of message
        /// </summary>
        public string Email { get; set; }
        
        /// <summary>
        /// Base64 encoded message
        /// </summary>
        public string Content { get; set; }
    }

    /// <summary>
    /// Modifier class to encode BigIntegers into Base64 and vice versa
    /// </summary>
    internal class Modifier
    {
        /// <summary>
        /// Decode the length of a byte array into a BigInt
        /// </summary>
        /// <param name="keyByte">Key byte array</param>
        /// <param name="sIndex">start index of 4 bytes for length</param>
        /// <returns></returns>
        private BigInteger DecodeLen( byte[] keyByte, int sIndex )
        {
            var lenByte = new byte[4];
            Array.Copy( keyByte, sIndex, 
                lenByte, 0, 4 );
            if ( BitConverter.IsLittleEndian )
            {
                Array.Reverse( lenByte, 0, lenByte.Length );
            }
            return new BigInteger( lenByte );
        }

        /// <summary>
        /// Decode the contents of a byte array into a BigInt
        /// </summary>
        /// <param name="bLen">length of the value</param>
        /// <param name="keyByte">Key byte array</param>
        /// <param name="sIndex">start index of value</param>
        /// <returns></returns>
        private BigInteger DecodeContents(BigInteger bLen, byte[] keyByte, int sIndex )
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

        /// <summary>
        /// Encode the length of a byte array into the main Key byte array
        /// </summary>
        /// <param name="source">byte array's length to put into main Key byte array</param>
        /// <param name="dIndex">destination index in byteArr to write to</param>
        /// <param name="byteArr">destination of main Key byte array</param>
        private void EncodeLen( byte[] source, int dIndex, ref byte[] byteArr )
        {
            var lenBytes = new byte[] { 0, 0, 0, 0 };
            var tempE = BitConverter.GetBytes( source.Length );
            Array.Copy(tempE, 0,
                lenBytes, 0, tempE.Length );
            if ( BitConverter.IsLittleEndian )
            {
                Array.Reverse( lenBytes, 0, lenBytes.Length );
            }
            Array.Copy( lenBytes, 0, 
                byteArr, dIndex, lenBytes.Length );
        }

        /// <summary>
        /// Encode the contents of a byte array into the main Key byte array
        /// </summary>
        /// <param name="source">byte array to put into main Key byte array</param>
        /// <param name="dIndex">destination index in byteArr to write to</param>
        /// <param name="byteArr">destination of main Key byte array</param>
        private void EncodeContents( byte[] source, int dIndex, ref byte[] byteArr )
        {
            var byteContents = new byte[ source.Length ];
            Array.Copy(source, 0, 
                byteContents, 0, source.Length );
            if ( !BitConverter.IsLittleEndian )
            {
                Array.Reverse( byteContents, 0, byteContents.Length );
            }
            Array.Copy( byteContents, 0, 
                byteArr, dIndex, byteContents.Length );
        }
        
        /// <summary>
        /// Decode a Base64 Key into a list of BigInt
        ///     - i = 0: E or D
        ///     - i = 1: N
        /// </summary>
        /// <param name="base64Key">Base64 key to decode</param>
        /// <returns>list of BigInt</returns>
        public List<BigInteger> DecodeKey( string base64Key )
        {
            var keyByte = Convert.FromBase64String( base64Key );  // big endian

            var eBigInt = DecodeLen( keyByte, 0 );  // length of e
            var e = DecodeContents( eBigInt, keyByte, 4 );  // contents of e
            var nBigInt = DecodeLen( keyByte, 4 + (int) eBigInt );  // length of n
            var n = DecodeContents( nBigInt, keyByte, 8 + (int) eBigInt );  // contents of n
            
            return new List<BigInteger> { e, n };
        }

        /// <summary>
        /// Encode two BitInt into a Base64 string
        /// </summary>
        /// <param name="e">e or d to be encoded</param>
        /// <param name="n">n to be encoded</param>
        /// <returns>Base64 encoded key</returns>
        public string EncodeKey( BigInteger e , BigInteger n )
        {
            var eByteSource = e.ToByteArray();  // little endian
            var nByteSource = n.ToByteArray();  // little endian

            var byteArr = new byte[ 8 + eByteSource.Length + nByteSource.Length ];

            EncodeLen( eByteSource, 0, ref byteArr );  // length of e
            EncodeContents( eByteSource, 4, ref byteArr );  // contents of e
            EncodeLen( nByteSource, 4 + eByteSource.Length, ref byteArr );  // length of n
            EncodeContents( nByteSource, 8 + eByteSource.Length, ref byteArr );  // contents of n
            
            return Convert.ToBase64String( byteArr );
        }
    }
    
    /// <summary>
    /// All of the required tasks for the project: keyGen, sendKey, getKey, sendMsg, getMsg
    /// </summary>
    internal class ReqTasks
    {
        /// <summary>
        /// Modifier object to modify the keys
        /// </summary>
        private readonly Modifier Mod = new Modifier();
        
        /// <summary>
        /// HttpClient to interact with the server
        /// </summary>
        private readonly HttpClient Client = new HttpClient();
        
        /// <summary>
        /// Generate a private and public key and store them on the disk as private.key and public.key
        ///     - Emails should be empty because they have not been sent out yet
        ///     - Keys should be Base64 encoded
        /// </summary>
        /// <param name="keySize">size of the requested key in bits</param>
        internal void GenerateKey( int keySize )
        {
            var primeGen = new PrimeGen();
            var rand = new Random();
            
            var factor = rand.Next( 5, 10 );
            var firstSize = ( keySize / 2 ) + rand.Next( -keySize / factor, keySize / factor );
            
            var p = primeGen.GenPrimeNum( firstSize, 1 );
            var q = primeGen.GenPrimeNum( keySize - firstSize, 1 );
            
            var n = p * q;
            var r = ( p - 1 ) * ( q - 1 );
            var e = primeGen.GenPrimeNum( 16, 1 );
            var d = Given.ModInverse( e, r );
            
            var publicKeyStr = Mod.EncodeKey( e, n );
            var privateKeyStr = Mod.EncodeKey( d, n );

            var publicKey = new Keys() { Email = "", Key = publicKeyStr };
            var privateKey = new PrivateKeys() { Emails = new List<string>(), Key = privateKeyStr };

            var publicKeyJson = JsonConvert.SerializeObject( publicKey, Formatting.Indented );
            var privateKeyJson = JsonConvert.SerializeObject( privateKey, Formatting.Indented );

            File.WriteAllText( "public.key", publicKeyJson );
            File.WriteAllText("private.key", privateKeyJson );
            
            Console.WriteLine("Successfully generated private and public keys and stored the on the disk!");
        }
        
        /// <summary>
        /// Register the public key under the email profile
        /// </summary>
        /// <param name="email">Profile where the public key will be registered</param>
        internal void SendKey( string email )
        {
            var privateKey = JsonConvert.DeserializeObject<PrivateKeys>(File.ReadAllText( "private.key" ));
            var publicKey = JsonConvert.DeserializeObject<Keys>( File.ReadAllText( "public.key" ) );
            
            publicKey.Email = email;
            
            var publicKeyJson = JsonConvert.SerializeObject( publicKey, Formatting.Indented );

            var response = Client.PutAsync( "http://kayrun.cs.rit.edu:5000/Key/" + email,
                new StringContent( publicKeyJson, Encoding.UTF8, "application/json" ) ).Result;
            response.EnsureSuccessStatusCode();

            privateKey.Emails.Add( email );
            var privateKeyJson = JsonConvert.SerializeObject( privateKey, Formatting.Indented );
            File.WriteAllText( "private.key", privateKeyJson );
            
            Console.WriteLine( "Sent key to {0}!", email );
        }
        
        /// <summary>
        /// Encodes a plaintext to base64 and sends it to the email's "mailbox"
        /// </summary>
        /// <param name="email">Email of mailbox to send to</param>
        /// <param name="plaintext">Text message to send to</param>
        /// <exception cref="FileNotFoundException">Cannot find person's public key to write message</exception>
        internal void SendMessage( string email, string plaintext )
        {
            try
            {
                var keyObj = JsonConvert.DeserializeObject<Keys>( File.ReadAllText( email + ".key" ) );
                
                var decodeEn = Mod.DecodeKey( keyObj.Key );
                var e = decodeEn[0];
                var n = decodeEn[1];
                
                Console.WriteLine( "E: {0}\nN: {1}", e, n );

                var textByte = Encoding.UTF8.GetBytes( plaintext );
                var bigText = new BigInteger( textByte );
                
                var cypherText = BigInteger.ModPow( bigText, e, n );
                
                var message = new Messages() { Email = email, 
                    Content = Convert.ToBase64String( cypherText.ToByteArray() ) };
                var messageJson = JsonConvert.SerializeObject( message, Formatting.Indented );

                var response = Client.PutAsync( "http://kayrun.cs.rit.edu:5000/Message/" + email,
                        new StringContent( messageJson, Encoding.UTF8, "application/json" ) ).Result;
                response.EnsureSuccessStatusCode();
            }
            catch ( FileNotFoundException )
            {
                throw new FileNotFoundException("Cannot find " + email + ".key to send message" );
            }
        }

        /// <summary>
        /// Receive a public key from an email location
        /// </summary>
        /// <param name="email">Email location to receive the public key from</param>
        internal void GetKey( string email )
        {
            var response = Client.GetAsync( "http://kayrun.cs.rit.edu:5000/Key/" + email ).Result;
            response.EnsureSuccessStatusCode();

            var jsonObj = response.Content.ReadAsStringAsync().Result;
            var keyObj = JsonConvert.DeserializeObject<Keys>( jsonObj );
            
            var newJsonObj = JsonConvert.SerializeObject( keyObj, Formatting.Indented );

            File.WriteAllText( email + ".key", newJsonObj );

            Console.WriteLine( "Successfully got key from {0}", email );
        }

        /// <summary>
        /// Receive message from an email location
        /// </summary>
        /// <param name="email">Email location to receive a message from</param>
        /// <exception cref="ArgumentException">Location is not registered in private.key</exception>
        internal void GetMessage( string email )
        {
            var response = Client.GetAsync("http://kayrun.cs.rit.edu:5000/Message/" + email ).Result;
            response.EnsureSuccessStatusCode();
            
            var jsonObj = response.Content.ReadAsStringAsync().Result;
            
            var messageObj = JsonConvert.DeserializeObject<Messages>( jsonObj );

            var privateKeyObj = JsonConvert.DeserializeObject<PrivateKeys>( File.ReadAllText("private.key" ) );
            
            if ( !privateKeyObj.Emails.Contains( email ) )
            {
                throw new ArgumentException( email + " was not found in private.key. " +
                                            "Please send " + email + " a key first" );
            }
            
            var decodeEn = Mod.DecodeKey( privateKeyObj.Key );
            var d = decodeEn[0];
            var n = decodeEn[1];

            var messageByte = Convert.FromBase64String( messageObj.Content );

            var cypherText = new BigInteger( messageByte );
            var plainText = BigInteger.ModPow( cypherText, d, n );

            var textBytes = plainText.ToByteArray();

            if ( !BitConverter.IsLittleEndian )
            {
                Array.Reverse( textBytes, 0, textBytes.Length );
            }
            
            Console.WriteLine( Encoding.UTF8.GetString( textBytes ) );
        }
    }
    
    public static class Program
    {
        /// <summary>
        /// Request object to complete requests
        /// </summary>
        private static readonly ReqTasks Request = new ReqTasks();
        
        /// <summary>
        /// Enum for user's options
        /// </summary>
        private enum Options
        {
            KeyGen,
            SendKey,
            GetKey,
            SendMsg,
            GetMsg,
            Invalid
        }
        
        /// <summary>
        /// Prints out the command line usage for the user
        /// </summary>
        /// <param name="variation">variation to print</param>
        private static void Usage( int variation = 0 )
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
                    Console.WriteLine( "{0,60}", "<keysize>: size of keys generated" );
                    break;
            }
            Environment.Exit( 1 );
        }
        
        /// <summary>
        /// Get the options from the command line string args
        /// </summary>
        /// <param name="arg">command line args</param>
        /// <returns>the option the user selects</returns>
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