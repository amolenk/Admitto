# Enabling HTTPS in ASP.NET using your own dev certificate

To enable HTTPS in ASP.NET, you can mount an exported copy of your local dev certificate.

Export it using the following command:

```bash
dotnet dev-certs https --trust; dotnet dev-certs https -ep "${HOME}/.aspnet/https/aspnetapp.pfx" -p "SecurePwdGoesHere"
```