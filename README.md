# HTTP Logging Sink
I originally made this to support me logging errors that were happening 
in my game where I can't easily log them away besides the console.

## How it works
It just takes in a log from either {api base}/log or {api base}/log/bulk

## How to setup (Ubuntu)
### Prerequisites:
 - Docker
 - Git
### Setup
1. Start out by making a folder so we can keep everything nice and organized
```shell
mkdir http-logging-sink-tutorial
cd http-logging-sink-tutorial
```
2. Then clone the repo into the folder
```shell
git clone https://github.com/SocksM/HttpLoggingSink
```
3. Now you probably want to write your `.env` file, there is an example layout included (`.env.example`) <br />
   So I'm just going to copy that file
```shell
cp HttpLoggingSink/.env.example .env
```
4. Before we modify our .env we also want to copy the appsettings that .Net will use
```shell
cp HttpLoggingSink/HttpLoggingSink/appsettings.json appsettings.json
```
5. Now lets edit our `appsettings.json`, imma set it to
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Trace",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ApiKeyAppNamePairs": {
    "TestKey": "SuperLongAndSecureApiKey"
  },
  "ElasticSearch": {
    "Uri": "http://localhost:9200",
    "Username": "elastic",
    "Password": "SuperLongAndSecureHttpLogSinkPassword"
  }
}
```
A bit more context about the appsettings:
- All ElasticSearch settings will be overwritten by the docker compose later.<br />
  These settings exist for if you for some reason want to launch it standalone. <br />
  So in actuality I shouldn't be setting my ElasticSearch settings here because I will be using the docker compose in this tutorial.
- LogLevel.Default should be put as low as you want your LogStash logs to go the levels are:
  0. "Trace"
  1. "Debug"
  2. "Information"
  3. "Warning"
  4. "Error"
  5. "Critical"
  6. <i>"None"</i>
6. Lets now modify the `.env`, Imma put it like this
```dotenv
# The password that you can use to log into kibana with
ELASTIC_PASSWORD=SuperLongAndSecureElasticPassword
# Is the password used that kibana will use to fetch the logs for you
KIBANA_SYSTEM_PASSWORD=SuperLongAndSecureKibanaPassword
# Is the password that will be used by the sink to log into elastic
HTTPLOGGINGSINK_PASSWORD=SuperLongAndSecureHttpLogSinkPassword
# Path to your appsettings.json
HTTP_LOGGING_SINK_CONFIG=/home/my-user/http-logging-sink-tutorial/appsettings.json
```
7. Now just copy the docker compose
```shell
cp HttpLoggingSink/docker-compose.yaml docker-compose.yaml
```
8. Okay that was that, you are nearly done! Just launch using:
```shell
docker compose up -d
```
The -d is for daemon, we probably dont want to keep it in the foreground lol
9. Now to see if it's all working we are going to send a test log.
```shell
curl -v `
  -X POST http://localhost:62847/log `
  -H "Content-Type: application/json" `
  -H "X-Api-Key: SuperLongAndSecureApiKey" `
  -d '{
    "logLevel": 2,
    "logMessage": "Test log with 2 test args: {str-arg} {funny-number}!"
    "logSource": "Test",
    "logArgs": ["test arg", 420]
  }'
```
<sub>Log level is a number to see what each number corresponds to see the list in step 5, in this case 2 is information </sub><br />
<br />
Now just log into kibana with the password set in .env (ELASTIC_PASSWORD) and the username "elastic"<br />
Kibana is hosted at http://localhost:5601 <br />

10. Optionally now delete the git repo from disk
```shell
rm -rf HttpLoggingSink
```
## How to shut down
If you want to just shut it down for now and <strong>KEEP YOUR DATA</strong> use
```shell
docker compose down
```
If you want to <strong>DELETE</strong> all the associated data add `-v`
