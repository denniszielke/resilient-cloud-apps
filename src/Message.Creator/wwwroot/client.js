var apiUrl = '/';
function loopAsyncClick() {
    console.log(document.getElementById('PublishMessage'));
    document.getElementById('PublishMessage').click();
};
function loopSyncClick() {
    console.log(document.getElementById('InvokeRequest'));
    document.getElementById('InvokeRequest').click();
};

function uuidv4() {
    return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function(c) {
        var r = Math.random() * 16 | 0, v = c == 'x' ? r : (r & 0x3 | 0x8);
        return v.toString(16);
    });
};

function logDate() {    
    var startDate = new Date();
    return startDate.toISOString();
    // var month = (((startDate.getMonth()+1)<10) ? '0' + (startDate.getMonth()+1) : (startDate.getMonth()+1));
    // var day = (((startDate.getDate())<10) ? '0' + (startDate.getDate()) : (startDate.getDate()));
    // var hour = (((startDate.getHours())<10) ? '0' + (startDate.getHours()) : (startDate.getHours()));
    // var minute = (((startDate.getMinutes())<10) ? '0' + (startDate.getMinutes()) : (startDate.getMinutes()));
    // var seconds = (((startDate.getSeconds())<10) ? '0' + (startDate.getSeconds()) : (startDate.getSeconds()));
    // var logDate = month+  "-" + day + " " + hour + ":" + minute + ":" + seconds; 
    // return logDate;
};

angular.module('SimulatorApp', [])
    .controller('SimulatorController',
        function ($scope, $http) {

            $scope.Init = function () {             
                var getUrl = apiUrl + 'getappinsightskey';
                var config = {
                    headers: {
                        'Accept': 'application/json',
                        'Content-Type': 'application/json'
                    }
                };
                $http.get(getUrl, {}, config)
                    .success(function (response) { 
                        $scope.appInsightsKey = response;
                        console.log(response);
                        initAppInsights($scope.appInsightsKey);
                    }); 

            };

            $scope.Version = function () {
                var postUrl = apiUrl + 'getversion';
                var config = {
                    headers: {
                        'Accept': 'application/json',
                        'Content-Type': 'application/json'
                    }
                };
                $http.get(postUrl, { 'getname': status }, config)
                    .success(function (response) { 
                        $scope.version = response;
                        $scope.result = "Connected";
                        console.log("received version:");
                        console.log(response);
                    });       
            };

            $scope.Name = function () {
                var postUrl = apiUrl + 'getname';
                var config = {
                    headers: {
                        'Accept': 'application/json',
                        'Content-Type': 'application/json'
                    }
                };
                $http.get(postUrl, { 'getname': status }, config)
                    .success(function (response) { 
                        $scope.name = response;
                        $scope.message = 'hi from ' + response;
                        $scope.humidity = 20;
                        $scope.temperature = 15;
                        $scope.result = "Connected";
                        $scope.responses =  [];
                        console.log("received version:");
                        console.log(response);
                    });       
            };

            $scope.CalculateCssClass = function(status){
                if (status && status != undefined){
                    if (status.toString().indexOf("200") >= 0)
                        return "bg-green";
                    else if (status.toString().indexOf("429") >= 0)
                        return "bg-yellow";
                    else if (status.toString().indexOf("500") >= 0)
                        return "bg-red";
                    else
                        return "bg-info";
                }
            }

            $scope.InvokeRequest = function () {
                var postUrl = apiUrl + 'receive';
                var uid = uuidv4();
                var logDateStr = logDate();
                $scope.requeststartDate = new Date();
                var config = {
                    headers: {
                        'Accept': 'application/json',
                        'Content-Type': 'application/json',
                        'id': uid,
                        'temperature': $scope.temperature,
                        'humidity': $scope.humidity,
                        'name': $scope.name,
                        'message': $scope.message,
                        'timestamp': logDateStr
                    }
                };
                var body = {
                    'id': uid,
                    'temperature': $scope.temperature,
                    'humidity': $scope.humidity,
                    'name': $scope.name,
                    'message': $scope.message,
                    'timestamp': logDateStr
                }
                console.log(config.headers);

                window.globalAppInsights.properties.context.telemetryTrace.traceID = Microsoft.ApplicationInsights.Telemetry.Util.generateW3CId().
                window.globalAppInsights.trackEvent({name:"InvokeRequest"});
                $http.post(postUrl, body, config)
                    .success(function (response) { 
                        var endDate = new Date();
                        response.sync = "S";
                        response.duration = endDate - $scope.requeststartDate
                        $scope.result = response;
                        $scope.responses.splice(0,0,response);
                        console.log("received response:");
                        console.log(response);  
                        if ($scope.loop){
                            window.setTimeout(loopSyncClick, 500);
                        }
                    });
            };

            $scope.PublishMessage = function () {
                var postUrl = apiUrl + 'publish';
                var uid = uuidv4();
                var logDateStr = logDate();
                $scope.requeststartDate = new Date();
                var config = {
                    headers: {
                        'Accept': 'application/json',
                        'Content-Type': 'application/json',
                        'id': uid,
                        'temperature': $scope.temperature,
                        'humidity': $scope.humidity,
                        'name': $scope.name,
                        'message': $scope.message,
                        'timestamp': logDateStr
                    }
                };

                var body = {
                    'id': uid,
                    'temperature': $scope.temperature,
                    'humidity': $scope.humidity,
                    'name': $scope.name,
                    'message': $scope.message,
                    'timestamp': logDateStr
                }
                console.log(config.headers);

                window.globalAppInsights.properties.context.telemetryTrace.traceID = Microsoft.ApplicationInsights.Telemetry.Util.generateW3CId().
                window.globalAppInsights.trackEvent({name:"PublishMessage"});
                $http.post(postUrl, body, config)
                    .success(function (response) { 
                        var endDate = new Date();
                        response.sync = "A";
                        response.duration = endDate - $scope.requeststartDate
                        $scope.result = response;
                        $scope.responses.splice(0,0,response);
                        console.log("received response:");
                        console.log(response);  
                        if ($scope.loop){
                            var randum = Math.floor((Math.random() * 5) + 1);
                            var randtm = Math.floor((Math.random() * 5) + 1);
                            if ($scope.humidity > 100 || $scope.humidity < 7)
                            {
                                $scope.humidity = 25;
                            }
                            if ($scope.temperature > 100 || $scope.temperature < 7)
                            {
                                $scope.temperature = 25;
                            }
                            if (randum > 3){
                                $scope.humidity += randum;
                                $scope.temperature -= randtm;
                            }
                            else{
                                $scope.humidity -= randum;
                                $scope.temperature += randtm;
                            }
                            window.setTimeout(loopAsyncClick, 500);
                        }
                    });
            };
            
            $scope.Init();
            $scope.Version();
            $scope.Name();
        }
    );