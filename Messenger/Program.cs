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
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(lenByte, 0, lenByte.Length);
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
            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(byteArr, 0, byteArr.Length);
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
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(lenBytes, 0, lenBytes.Length);
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
            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(byteContents, 0, byteContents.Length);
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
        internal List<BigInteger> DecodeKey( string base64Key )
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
        internal string EncodeKey( BigInteger e , BigInteger n )
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
        private readonly Modifier _mod = new Modifier();
        
        /// <summary>
        /// HttpClient to interact with the server
        /// </summary>
        private readonly HttpClient _client = new HttpClient();
        
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
            var e = primeGen.GenPrimeNum( 32, 1 );
            var d = Given.ModInverse( e, r );
            
            var publicKeyStr = _mod.EncodeKey( e, n );
            var privateKeyStr = _mod.EncodeKey( d, n );

            var publicKey = new Keys() { Email = "", Key = publicKeyStr };
            var privateKey = new PrivateKeys() { Emails = new List<string>(), Key = privateKeyStr };

            var publicKeyJson = JsonConvert.SerializeObject( publicKey, Formatting.Indented );
            var privateKeyJson = JsonConvert.SerializeObject( privateKey, Formatting.Indented );

            File.WriteAllText( "public.key", publicKeyJson );
            File.WriteAllText("private.key", privateKeyJson );
            
            Console.WriteLine( "Successfully generated private and public keys and stored the on the disk!" );
        }
        
        /// <summary>
        /// Register the public key under the email profile
        /// </summary>
        /// <param name="email">Profile where the public key will be registered</param>
        internal void SendKey( string email )
        {
            try
            {
                var privateKey = JsonConvert.DeserializeObject<PrivateKeys>(File.ReadAllText( "private.key" ) );
                var publicKey = JsonConvert.DeserializeObject<Keys>(File.ReadAllText( "public.key" ) );

                publicKey.Email = email;

                var publicKeyJson = JsonConvert.SerializeObject( publicKey, Formatting.Indented );

                var response = _client.PutAsync( "http://kayrun.cs.rit.edu:5000/Key/" + email,
                    new StringContent( publicKeyJson, Encoding.UTF8, "application/json" ) ).Result;
                response.EnsureSuccessStatusCode();

                if ( !privateKey.Emails.Contains( email ) ) privateKey.Emails.Add( email );
                var privateKeyJson = JsonConvert.SerializeObject( privateKey, Formatting.Indented );

                File.WriteAllText( "private.key", privateKeyJson );

                Console.WriteLine( "Sent key to {0}!", email );
            }
            catch ( FileNotFoundException )
            {
                throw new FileNotFoundException( 
                    "Cannot find public or private key, please generate a key first" );
            }
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
                
                var decodeEn = _mod.DecodeKey( keyObj.Key );
                var e = decodeEn[0];
                var n = decodeEn[1];

                var textByte = Encoding.UTF8.GetBytes( plaintext );
                var bigText = new BigInteger( textByte );
                
                var cypherText = BigInteger.ModPow( bigText, e, n );
                
                var message = new Messages() { Email = email, 
                    Content = Convert.ToBase64String( cypherText.ToByteArray() ) };
                var messageJson = JsonConvert.SerializeObject( message, Formatting.Indented );

                var response = _client.PutAsync( "http://kayrun.cs.rit.edu:5000/Message/" + email,
                        new StringContent( messageJson, Encoding.UTF8, "application/json" ) ).Result;
                response.EnsureSuccessStatusCode();
            }
            catch ( FileNotFoundException )
            {
                throw new FileNotFoundException(
                    "Cannot find {0}.key to send message. Please download the public key first.", email );
            }
            
            Console.WriteLine( "Sent message to {0}.", email );
        }

        /// <summary>
        /// Receive a public key from an email location
        /// </summary>
        /// <param name="email">Email location to receive the public key from</param>
        internal void GetKey( string email )
        {
            var response = _client.GetAsync( "http://kayrun.cs.rit.edu:5000/Key/" + email ).Result;
            response.EnsureSuccessStatusCode();

            var jsonObj = response.Content.ReadAsStringAsync().Result;
            var keyObj = JsonConvert.DeserializeObject<Keys>( jsonObj );

            if ( keyObj == null )
            {
                throw new ArgumentException( "Key received was null. Please try again or check email." );
            }
            
            var newJsonObj = JsonConvert.SerializeObject( keyObj, Formatting.Indented );

            File.WriteAllText( email + ".key", newJsonObj );

            Console.WriteLine( "Successfully got key from {0}.", email );
        }

        /// <summary>
        /// Receive message from an email location
        /// </summary>
        /// <param name="email">Email location to receive a message from</param>
        /// <exception cref="ArgumentException">Location is not registered in private.key</exception>
        internal void GetMessage( string email )
        {
            try
            {
                var response = _client.GetAsync( "http://kayrun.cs.rit.edu:5000/Message/" + email ).Result;
                response.EnsureSuccessStatusCode();

                var jsonObj = response.Content.ReadAsStringAsync().Result;

                var messageObj = JsonConvert.DeserializeObject<Messages>( jsonObj );

                var privateKeyObj = JsonConvert.DeserializeObject<PrivateKeys>( File.ReadAllText( "private.key" ) );

                if ( !privateKeyObj.Emails.Contains( email) )
                {
                    throw new ArgumentException( "Message could not be decoded." );
                }

                var decodeEn = _mod.DecodeKey( privateKeyObj.Key );
                var d = decodeEn[0];
                var n = decodeEn[1];

                var messageByte = Convert.FromBase64String( messageObj.Content );

                var cypherText = new BigInteger( messageByte );
                var plainText = BigInteger.ModPow(cypherText, d, n );

                var textBytes = plainText.ToByteArray();

                if ( !BitConverter.IsLittleEndian )
                {
                    Array.Reverse( textBytes, 0, textBytes.Length );
                }

                Console.WriteLine( Encoding.UTF8.GetString( textBytes ) );
            }
            catch ( FileNotFoundException )
            {
                throw new FileNotFoundException( 
                    "Private key not found, please generate a key and send it out first.");
            }
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
        /// <param name="opt">user options to describe</param>
        private static void Usage( Options opt )
        {
            switch ( opt )
            {
                case Options.KeyGen:
                    Console.WriteLine( "{0}\n{1}\n{2}\n{3}", 
                        "Usage: keyGen <keysize>",
                        "<keysize>: total combined size in bits for both the public and private key", 
                        "- public key stored as 'public.key'",
                        "- private key stored as 'private.key'" );
                    break;
                case Options.SendKey:
                    Console.WriteLine( "{0}\n{1}", 
                        "Usage: sendKey <email>",
                        "<email>: sends the public key to the server and registers <email>" );
                    break;
                case Options.GetKey:
                    Console.WriteLine( "{0}\n{1}\n{2}", 
                        "Usage: getKey <email>",
                        "<email>: retrieve a base64 encoded public key for the <email> user",
                        "- stored as <email>.key" );
                    break;
                case Options.SendMsg:
                    Console.WriteLine( "{0}\n{1}\n{2}", 
                        "Usage: sendMsg <email> <plaintext>",
                        "<email>: recipient of message",
                        "<plaintext>: message to be base64 encoded and sent out" );
                    break;
                case Options.GetMsg:
                    Console.WriteLine( "{0}\n{1}", 
                        "Usage: getMsg <email>",
                        "<email>: retrieve base64 encoded message from <email> and display it." );
                    break;
                case Options.Invalid:
                    Console.WriteLine( "{0}\n{1}", 
                        "Usage: dotnet run <option> <other arguments>",
                        "<option>: keyGen, sendKey, getKey, sendMsg, getMsg" );
                    break;
                default:
                    Console.WriteLine( "{0}\n{1}", 
                        "Usage: dotnet run <option> <other arguments>",
                        "<option>: keyGen, sendKey, getKey, sendMsg, getMsg" );
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
            if ( args.Length < 1 ) Usage( Options.Invalid );
            var option = GetOptions( args[0] );
            if ( option == Options.Invalid ) Usage( Options.Invalid );

            switch ( option )
            {
                case Options.KeyGen:
                    if ( args.Length != 2 ) Usage( Options.KeyGen );
                    Request.GenerateKey( Convert.ToInt32( args[1] ) );
                    break;
                case Options.SendKey:
                    if ( args.Length != 2 ) Usage( Options.SendKey );
                    Request.SendKey( args[1] );
                    break;
                case Options.GetKey:
                    if ( args.Length != 2 ) Usage( Options.GetKey );
                    Request.GetKey( args[1] );
                    break;
                case Options.SendMsg:
                    if ( args.Length != 3 ) Usage( Options.SendMsg );
                    Request.SendMessage( args[1], args[2] );
                    break;
                case Options.GetMsg:
                    if ( args.Length != 2 ) Usage( Options.GetMsg );
                    Request.GetMessage( args[1] );
                    break;
                case Options.Invalid:
                    Usage( Options.Invalid );
                    break;
                default:
                    Usage( Options.Invalid );
                    break;
            }
        }
    }
}