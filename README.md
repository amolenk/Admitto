# Admitto
Open source ticketing system for small, free events

## Run locally

### Cosmos DB Emulator

To use the Cosmos DB emulator without disabling TLS/SSL on the client, you need to import the emulator's certificate.
Below are instructions for macOS:

1. Export the certificate from the emulator:

   ```
   openssl s_client -connect localhost:5501 </dev/null | sed -ne '/-BEGIN CERTIFICATE-/,/-END CERTIFICATE-/p' > ~/cosmos_emulator.cert
   ```

2. Open KeyChain Access application. Click *File* -> *Import Items* -> *cosmos-emulator.cert*.

3. Right-click on the certificate and select *Get Info*, a pop-up window will open.
4. Under *Trust*, select *Always Trust*.
