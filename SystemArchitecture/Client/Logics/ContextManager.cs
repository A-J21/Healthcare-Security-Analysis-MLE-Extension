using System;
using Microsoft.Research.SEAL;

namespace CDTS_PROJECT.Logics
{
	/// ContextManager: Manages the encryption context and parameters for homomorphic encryption, think of the last class she taught.
	/// 
	/// WHAT IT DOES:
	/// This class sets up the encryption scheme parameters that control how data is encrypted.
	/// Think of it like configuring a safe - you need to set the combination (parameters) before you can use it.
	/// 
	/// KEY CONCEPTS:
	/// - Homomorphic encryption allows us to perform calculations on encrypted data without decrypt it first
	/// - The SEAL library (Microsoft Research) the researchers reference is what  provides the encryption functionality
	/// - BFV (Brakerski-Fan-Vercauteren) is the specific encryption scheme we're using
	public class ContextManager
	{
		// Stores the encryption parameters (like the "settings" for our encryption scheme)
		public EncryptionParameters EncryptionParams { get; set; }
		
		// The actual encryption context that will be used for all encryption/decryption operations
		// This is created from the parameters above
		public SEALContext Context { get; set; }
		
		/// Constructor: Initializes the encryption parameters and creates the encryption context.
		public ContextManager()
		{
			// BFV is a homomorphic encryption scheme that allows addition and multiplication on encrypted data
			EncryptionParams = new EncryptionParameters(SchemeType.BFV);
			
			// Polynomial modulus degree: Controls the security level and performance
			// 2048 is a good balance that they went with provides strong security while being computationally manageable
			// Higher values = more secure but slower operations
			const ulong polyModulusDegree = 2048;
			EncryptionParams.PolyModulusDegree = polyModulusDegree;
			
			// Coefficient modulus: Additional security parameters
			// Uses default values recommended for the chosen polynomial modulus degree
			// This ensures the encryption is secure and operations won't cause errors
			EncryptionParams.CoeffModulus = CoeffModulus.BFVDefault(polyModulusDegree);
			
			// Plain modulus: The size of the number space we can encrypt
			// 1024 means we can encrypt integers in the range [0, 1024)
			EncryptionParams.PlainModulus = new Modulus(1024);
			
			// Create the encryption context from our parameters
			// This context object will be used for all encryption, decryption, and computation operations
			// It validates that our parameters are correct and sets up the internal encryption machinery
			Context = new SEALContext(EncryptionParams);
		}

	}
}
