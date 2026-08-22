# Enabling HTTPS in ASP.NET using your own dev certificate

To enable HTTPS in ASP.NET inside the devcontainer, mount an exported copy of your local dev certificate.

## 1. On the host

Export your dev cert (once per machine, or whenever it expires):

```bash
dotnet dev-certs https --trust
dotnet dev-certs https -ep "${HOME}/.aspnet/https/aspnetapp.pfx" -p "SecurePwdGoesHere"
```

Set `ASPNETAPP_HTTPS_PASSWORD` in your host environment to the same password — `devcontainer.json` passes it into the container via `remoteEnv`.

## 2. Inside the container

`devcontainer.json` bind-mounts `~/.aspnet/https` (read-only) so Kestrel can serve HTTPS using this cert. 

`postStartCommand` runs `.devcontainer/trust-dev-cert.sh` on container start, which extracts the actual certificate from `aspnetapp.pfx` and installs it into the system CA trust store via `update-ca-certificates`. This is what makes `HttpClient` (and everything else that validates certs against the system store) trust connections to the API's HTTPS endpoint.

If you ever see `AuthenticationException: ... UntrustedRoot` when calling the API over HTTPS from inside the container, re-run:

```bash
bash .devcontainer/trust-dev-cert.sh
```
