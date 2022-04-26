using System;
using System.Text;

public class SecretsConfig
{
    public string Username { get; set; }
    public string AuthorizationToken { get; set; }

    public string AsBase64Token()
    {
        var token = $"{this.Username}:{this.AuthorizationToken}";

        return Convert.ToBase64String(Encoding.UTF8.GetBytes(token));
    }
}
