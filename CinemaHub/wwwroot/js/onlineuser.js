//get users online
//
"user strict";

var conn = new signalR.HubConnectionBuilder().withUrl("/UsersOnlineHub").build();
//start connection
conn.start().then(function () {

    console.log("start");

}).catch(
    function (err) {
        console.error(err.toString());
    }
);
//receive msg from server

// chart
var chartConfig = {
    type: 'line',
    data: {
        labels: [],
        datasets: [{
            label: 'Online Users',
            data: [],
            backgroundColor: 'rgba(54, 162, 235, 0.2)',
            borderColor: 'rgba(54, 162, 235, 1)',
            borderWidth: 1
        }]
    },
    options: {
        scales: {
            y: {
                beginAtZero: true
            }
        }
    }
};
var onlineUsersChart = new Chart(document.getElementById('online-user-chart'), chartConfig);

let previousCount = 0;
let updateInterval;
conn.on("GetUsersCounter", function (UsersCounter) {
    const authen = UsersCounter.authen;
    const guest = UsersCounter.guest;
    document.querySelector(".online-user-number").innerHTML = authen;
    document.querySelector(".online-guest-number").innerHTML = guest;
    if (previousCount !== UsersCounter) {
        clearInterval(updateInterval);
    }
    updateInterval = window.setInterval(function () {
        updateOnlineUsers(UsersCounter)
    }, 2000);
    function updateOnlineUsers(UserCounter) {
        //document.querySelector(".online-user-number").innerHTML = UsersCounter;

        // Append new data point to the chart
        onlineUsersChart.data.labels.push(new Date().toLocaleTimeString());
        onlineUsersChart.data.datasets[0].data.push(authen);

        // Remove the first data point if the chart exceeds a certain number of points
        if (onlineUsersChart.data.labels.length > 10) {
            onlineUsersChart.data.labels.shift();
            onlineUsersChart.data.datasets[0].data.shift();
        }
        // Update the chart
        onlineUsersChart.update();
        previousCount = UserCounter;
    }
})



