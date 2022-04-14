'use strict';

const express = require('express');
const bodyParser = require('body-parser');
const fetch = require('cross-fetch');
const config = require('./config');
const { EventHubProducerClient } = require("@azure/event-hubs");
const OS = require('os');
const app = express();

var appInsights = require("applicationinsights");
if (config.instrumentationKey){ 
    appInsights.setup(config.instrumentationKey)
    .setAutoDependencyCorrelation(true)
    .setAutoCollectDependencies(true)
    .setAutoCollectPerformance(true)
    .setSendLiveMetrics(true)
    .setDistributedTracingMode(appInsights.DistributedTracingModes.AI_AND_W3C);
    appInsights.defaultClient.context.tags[appInsights.defaultClient.context.keys.cloudRole] = "message-creator";
    appInsights.start();
    var client = appInsights.defaultClient;
    client.commonProperties = {
        slot: config.version
    };
}

const producerClient = new EventHubProducerClient(config.eventhubConnectionString, config.eventhubName);

var startDate = new Date();
var month = (((startDate.getMonth()+1)<10) ? '0' + (startDate.getMonth()+1) : (startDate.getMonth()+1));
var day = (((startDate.getDate())<10) ? '0' + (startDate.getDate()) : (startDate.getDate()));
var hour = (((startDate.getHours())<10) ? '0' + (startDate.getHours()) : (startDate.getHours()));
var minute = (((startDate.getMinutes())<10) ? '0' + (startDate.getMinutes()) : (startDate.getMinutes()));
var seconds = (((startDate.getSeconds())<10) ? '0' + (startDate.getSeconds()) : (startDate.getSeconds()));
var logDate = month+  "-" + day + " " + hour + ":" + minute + ":" + seconds; 

var publicDir = require('path').join(__dirname, '/public');
app.use(express.static(publicDir));
var jsonParser = bodyParser.json();

app.get('/ping', function(req, res) {
    console.log('received ping');
    var sourceIp = req.connection.remoteAddress;
    var pong = { response: "pong!", host: OS.hostname(), sourceip: sourceIp, version: config.version };
    console.log(pong);
    res.send(pong);
});

app.get('/healthz', function(req, res) {
    res.send('OK');
});

app.get('/api/getappinsightskey', function(req, res) {
    console.log('returned app insights key');
    if (config.instrumentationKey){ 
        res.send(config.instrumentationKey);
    }
    else{
        res.send('');
    }
});

app.get('/api/getversion', function(req, res) {
    console.log('received version');
    var response = "Started: " + logDate + ", host: " + OS.hostname() + ", version: " + config.version;
    console.log(response);
    res.send(response);
});

app.post('/api/getname', (_req, res) => {
    const names = [ "Peter", "Steve", "Bill", "Dave", "Tom", "Tim", "Dale", "Ben", "Andy", "Mike", "Anne", "Cat", "Maria", "Lucy", "Kye", "Paula", "Lena", "Kelly", "Ringo", "Matt"];
    var name = names[Math.floor(Math.random() * names.length)];
    res.send(name);
});

app.post('/api/newdevice', jsonParser, async (req, res) => {
    console.log("received new device request:");
    console.log(req.body);
    const producerClient = new EventHubProducerClient(config.eventhubConnectionString, config.eventhubName);
    const eventDataBatch = await producerClient.createBatch();
    let wasAdded = await eventDataBatch.tryAdd({ body: req.body });
    if (!wasAdded) {
        res.sendStatus(500);
    }
    await producerClient.sendBatch(eventDataBatch);
    res.sendStatus(200);
});

app.post('/api/invokerequest', jsonParser, async (req, res) => {
    console.log("received new device request:");
    console.log(req.body);
    fetch(config.receiverUrl, {
        method: 'POST',
        headers: {
            "Content-Type": "application/json",
            "Accept": "application/json"
        },
        body: JSON.stringify(req.body)
    }).then((response) => {
        if (!response.ok) {
            console.log("Failed to call");
        }
        else{
            console.log(response);
        }

        return response.json();        
    }).then((text) => {
        console.log(text);
        res.status(200).send(text);
    }).catch((error) => {
        console.log("failed to call " + config.receiverUrl);
        console.log(error);
        res.status(500).send({message: error});
    });
});

app.post('/api/publishmessage', jsonParser, async(req, res) => {
    console.log("publish new message:");
    console.log(req.body);
    const producerClient = new EventHubProducerClient(config.eventhubConnectionString, config.eventhubName);
    const eventDataBatch = await producerClient.createBatch();
    let wasAdded = await eventDataBatch.tryAdd({ body: req.body });
    if (!wasAdded) {
        res.sendStatus(500);
    }
    await producerClient.sendBatch(eventDataBatch);
    res.sendStatus(200);
});

console.log(OS.hostname());
app.listen(config.port, () => console.log(`Node App listening on port ${config.port}!`));
