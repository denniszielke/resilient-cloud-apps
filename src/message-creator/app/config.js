var config = {}

config.instrumentationKey = process.env.INSTRUMENTATIONKEY;
if (config.instrumentationKey && config.instrumentationKey == "dummyValue")
{
    config.instrumentationKey = null;
}
config.port = process.env.PORT || 3000;
config.version = "default - latest";

if (process.env.VERSION && process.env.VERSION.length > 0)
{
    console.log('found version environment variable');
    config.version = process.env.VERSION;
}
else
{
    config.version = "no version";
}

config.invocationUrl = process.INVOCATION_URL;

config.eventhubConnectionString = process.env.EVENTHUB_CONNECTIONSTRING;
config.eventhubName = process.env.EVENTHUB_NAME;

module.exports = config;