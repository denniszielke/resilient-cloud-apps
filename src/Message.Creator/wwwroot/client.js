var apiUrl = '/';
function loopClick() {
    console.log(document.getElementById('PublishMessage'));
    document.getElementById('PublishMessage').click();
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
                        $scope.result = "Connected";
                        initAppInsights($scope.appInsightsKey);
                    }); 

            };

            $scope.Stats = function () {
                var postUrl = apiUrl + 'app/getname';
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

            $scope.InvokeRequest = function () {
                var postUrl = apiUrl + 'api/receive';
                var uid = uuidv4();
                var logDateStr = logDate();
                var config = {
                    headers: {
                        'Accept': 'application/json',
                        'Content-Type': 'application/json',
                        'Ce-Specversion': '1.0',
                        'Ce-Type': 'dapr-demo',
                        'Ce-Source' : 'message-creator',
                        'Ce-Id': uid,
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
                $http.post(postUrl, body, config)
                    .success(function (response) { 
                        $scope.result = response;
                        console.log("received response:");
                        console.log(response);  
                        if ($scope.loop){
                            window.setTimeout(loopClick, 500);
                        }
                    });
            };

            $scope.PublishMessage = function () {
                var postUrl = apiUrl + 'api/publish';
                var uid = uuidv4();
                var logDateStr = logDate();
                var config = {
                    headers: {
                        'Accept': 'application/json',
                        'Content-Type': 'application/json',
                        'Ce-Specversion': '1.0',
                        'Ce-Type': 'dapr-demo',
                        'Ce-Source' : 'message-creator',
                        'Ce-Id': uid,
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
                $http.post(postUrl, body, config)
                    .success(function (response) { 
                        $scope.result = response;
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
                            window.setTimeout(loopClick, 500);
                        }
                    });
            };
            
            $scope.Init();
            $scope.Stats();
        }
    );