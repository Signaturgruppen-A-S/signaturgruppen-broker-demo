# Nets eID Broker .net core DEMO

Integration clients for the Nets MitID Broker.

## MitID testusers
See https://pp.mitid.dk/test-tool/frontend/#/create-identity for administration of MitID test users.

## Signing and encryption of requests
in the appsettings, both signing and encryption can be enabled by setting the appropriate bool enablers to true.
Always consider the need and requirements for your application and integration before enabling signing and encryption.

## Postman collection for simple OIDC requests
1: Get authorization code via a flow, e.g. using https://openidconnect.net (see postman example URL in postman collection below)

2: Use postman to invoke Token endpoint and Userinfo endpoint

https://github.com/Signaturgruppen-A-S/nets-eID-broker-demo/blob/main/postman/Nets%20eID%20Broker%20(PP).postman_collection%20(3).json
