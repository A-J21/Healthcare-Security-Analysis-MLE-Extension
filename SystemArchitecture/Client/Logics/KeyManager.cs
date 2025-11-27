using System;
using System.IO;
using Microsoft.Research.SEAL;

namespace CDTS_PROJECT.Logics
{
    /// IKeyManager: Interface defining what key management operations we need.
    /// 
    /// WHY INTERFACES:
    /// Interfaces define a "contract" which just  says "any class that implements this must have these methods"
    /// This makes the code more flexible and testable.
    public interface IKeyManager
    {
        SecretKey LoadSecretKey();
        PublicKey LoadPublicKey();
        KeyPair CreateKeys();
    }

    /// KeyPair: A simple container class that holds both the public and secret keys together.
    /// 
    /// KEY CONCEPTS:
    /// - Public Key: Can be shared with anyone - used to encrypt data
    /// - Secret Key: Must be kept private - used to decrypt data
    /// - In homomorphic encryption, the server gets the public key but never sees the secret key
    public class KeyPair{

        // Public key: Used for encryption, can be safely shared with the server
        public PublicKey publicKey { get;}
        
        // Secret key: Used for decryption, must NEVER leave the client
        public SecretKey secretKey { get;}

        /// Constructor: Creates a KeyPair from provided public and secret keys.
        public KeyPair( PublicKey _publicKey, SecretKey _secretKey){
            publicKey = _publicKey;
            secretKey = _secretKey;
        }

        
    }

    /// KeyManager: Manages encryption keys for homomorphic encryption.
    /// 
    /// WHAT IT DOES:
    /// This class handles creating new encryption keys or loading existing ones from files.
    /// In their system, we always create new keys for each session (for security).
    /// 
    /// HOW IT WORKS:
    /// - CreateKeys(): Generates a new pair of encryption keys
    /// - LoadPublicKey() / LoadSecretKey(): Load keys from files (not used in current implementation)
    public class KeyManager : IKeyManager

    {
        // We need the encryption context to create or load keys
        // The context contains the encryption parameters we set up in ContextManager
        private readonly ContextManager _contextManager;
        
        /// Constructor: Takes a ContextManager so we can use the encryption context.
        public KeyManager(ContextManager contextManager)
        {
            _contextManager = contextManager;
        }

        /// LoadPublicKey: Loads a public key from a file (pk.txt).
        /// 
        /// NOTE: This method is not currently used in our implementation.
        /// We generate new keys for each session instead.
        public PublicKey LoadPublicKey()
        {
            // Create a new PublicKey object
            PublicKey pk = new PublicKey();
            
            // Read the key data from the file into a memory stream
            MemoryStream memoryStream = new MemoryStream(File.ReadAllBytes("pk.txt"));
            
            // Load the key from the stream using our encryption context
            // The context ensures the key is compatible with our encryption parameters
            pk.Load(_contextManager.Context, memoryStream);
            
            return pk;
        }

        /// LoadSecretKey: Loads a secret key from a file (sk.txt).
        /// 
        /// NOTE: This method is not currently used in our implementation.
        /// We generate new keys for each session instead.
        public SecretKey LoadSecretKey()
        {
            // Create a new SecretKey object
            SecretKey sk = new SecretKey();
            
            // Read the key data from the file into a memory stream
            MemoryStream memoryStream2 = new MemoryStream(File.ReadAllBytes("sk.txt"));
            
            // Load the key from the stream using our encryption context
            sk.Load(_contextManager.Context, memoryStream2);
            return sk;
        }
            
        /// CreateKeys: Generates a new pair of encryption keys.
        /// 
        /// THIS IS THE METHOD WE ACTUALLY USE:
        /// Every time the client runs, it creates a fresh set of keys.
        /// This ensures maximum security - even if keys are somehow compromised, they're only good for one session.
        /// 
        /// HOW IT WORKS:
        /// 1. Create a KeyGenerator using our encryption context
        /// 2. The generator automatically creates both a public key (for encryption) and secret key (for decryption)
        /// 3. Return both keys wrapped in a KeyPair object
        public KeyPair CreateKeys()
        {
            // Create a key generator using our encryption context
            // The context tells the generator what encryption scheme and parameters to use
            KeyGenerator keyGenerator = new KeyGenerator(_contextManager.Context);
            
            // Extract the public key (can be shared with server)
            PublicKey pk = keyGenerator.PublicKey;
            
            // Extract the secret key (must stay on client, never sent to server)
            SecretKey sk = keyGenerator.SecretKey;

            // Return both keys together in a KeyPair object
            return new KeyPair(pk, sk);
        }
    }
}
