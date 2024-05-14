(function ($) {
    "use strict";

    // Spinner
    var spinner = function () {
        setTimeout(function () {
            if ($('#spinner').length > 0) {
                $('#spinner').removeClass('show');
            }
        }, 1);
    };
    spinner();


    // Back to top button
    $(window).scroll(function () {
        if ($(this).scrollTop() > 300) {
            $('.back-to-top').fadeIn('slow');
        } else {
            $('.back-to-top').fadeOut('slow');
        }
    });
    $('.back-to-top').click(function () {
        $('html, body').animate({ scrollTop: 0 }, 1500, 'easeInOutExpo');
        return false;
    });


    // Sidebar Toggler
    $('.sidebar-toggler').click(function () {
        $('.sidebar, .content').toggleClass("open");
        return false;
    });


    // Progress Bar
    $('.pg-bar').waypoint(function () {
        $('.progress .progress-bar').each(function () {
            $(this).css("width", $(this).attr("aria-valuenow") + '%');
        });
    }, { offset: '80%' });


    // Calender
    $('#calender').datetimepicker({
        inline: true,
        format: 'L'
    });


    // Testimonials carousel
    $(".testimonial-carousel").owlCarousel({
        autoplay: true,
        smartSpeed: 1000,
        items: 1,
        dots: true,
        loop: true,
        nav: false
    });

    // Tickets Per day 
    $.ajax({
        url: "/Admin/Dashboard/GetAllTickets",
        method: "GET",
        dataType: "json",
        success: function (response) {
            const ctx2 = $("#tickets-per-day").get(0).getContext("2d");
            // Extract data from the response
            const tickets = response.data;

            // Aggregate data by day          
            const dailyTickets = {};
            tickets.forEach(ticket => {
                const date = new Date(ticket.bookedDate).toLocaleDateString();
                if (!dailyTickets[date]) {
                    dailyTickets[date] = 0;
                }
                dailyTickets[date]++;
            });
            const sortedDates = Object.keys(dailyTickets).sort((a, b) => new Date(a) - new Date(b));
            var myChart2 = new Chart(ctx2, {
                type: "bar",
                data: {
                    labels: sortedDates,
                    datasets: [{
                        label: "Tickets",
                        data: sortedDates.map(date => dailyTickets[date]),
                        backgroundColor: "#0d6efd",
                        borderColor: "#0d6efd",
                        borderWidth: 1,
                        fill: false,
                        barPercentage: 0.4

                    }
                    ]
                },
                options: {
                    indexAxis: 'x',
                    scales: {
                        x: {
                            beginAtZero: true
                        }
                    }
                }
            });
        },
        error: function (xhr, status, error) {
            console.error("Error fetching data:", error);
        }
    });
    //Revenue per day

    $.ajax({
        url: "/Admin/Dashboard/GetAllTickets",
        method: "GET",
        dataType: "json",
        success: function (response) {
            const ctx2 = $("#revenue-per-day").get(0).getContext("2d");
            // Extract data from the response
            const tickets = response.data;

            // Aggregate data by day
            const dailyTotals = {};
            tickets.forEach(ticket => {
                const date = new Date(ticket.bookedDate).toLocaleDateString();
                if (!dailyTotals[date]) {
                    dailyTotals[date] = 0;
                }
                dailyTotals[date] += ticket.total;
            });
            const sortedDates = Object.keys(dailyTotals).sort((a, b) => new Date(a) - new Date(b));
            var myChart2 = new Chart(ctx2, {
                type: "bar",
                data: {
                    labels: sortedDates,
                    datasets: [{
                        label: "Revenue",
                        data: sortedDates.map(date => dailyTotals[date]),
                        backgroundColor: "#0d6efd",
                        borderColor: "#0d6efd",
                        borderWidth: 1,
                        fill: false,
                        barPercentage: 0.4
                    }
                    ]
                },
                options: {
                    indexAxis: 'x',
                    scales: {
                        x: {
                            beginAtZero: true
                        }
                    }
                }
            });
        },
        error: function (xhr, status, error) {
            console.error("Error fetching data:", error);
        }
    });


    //// Single Line Chart
    //var ctx3 = $("#line-chart").get(0).getContext("2d");
    //var myChart3 = new Chart(ctx3, {
    //    type: "line",
    //    data: {
    //        labels: [50, 60, 70, 80, 90, 100, 110, 120, 130, 140, 150],
    //        datasets: [{
    //            label: "Salse",
    //            fill: false,
    //            backgroundColor: "rgba(0, 156, 255, .3)",
    //            data: [7, 8, 8, 9, 9, 9, 10, 11, 14, 14, 15]
    //        }]
    //    },
    //    options: {
    //        responsive: true
    //    }
    //});
    $.ajax({
        url: "/Admin/Dashboard/GetTotalCustomers",
        method: "GET",
        success: function (response) {
            var totalCustomers = $(".total-customers");
            totalCustomers.html(response.count.toString());
        }
    })
    $.ajax({
        url: "/Admin/Dashboard/GetTotalTickets",
        method: "GET",
        success: function (response) {
            var totalCustomers = $(".total-tickets");
            totalCustomers.html(response.totalTickets.toString());
        }
    })
    $.ajax({
        url: "/Admin/Dashboard/GetTotalRevenue",
        method: "GET",
        success: function (response) {
            var totalCustomers = $(".total-revenue");
            totalCustomers.html(response.totalRevenue.toString());
        }
    })
    $.ajax({
        url: `/Admin/Dashboard/GetTrendingMovies`,
        method: "GET",
        success: function (response) {
            var trendingMovies = $(".trending-movies");
            console.log(response);
            trendingMovies.empty(); // Clear existing content
            $.each(response.data, function (index, data) {
                var movieName = data.movie;
                var totalRevenue = data.totalRevenue;
                trendingMovies.append(`<div class="d-flex align-items-center border-bottom py-3">                  
                    <div class="w-100 ms-3">
                        <div class="d-flex w-100 justify-content-between">
                            <h5 class="mb-0">${movieName}</h5>
                            <h6 class="pb-0">Revenue: $${totalRevenue}</h6>
                        </div>
                    </div>
                </div >`);
            });
        }
    })
    $(document).ready(function () {
        // Attach change event handler to the select element
        $("#floatingSelect").change(function () {
            // Get the selected option value
            var selectedOption = $(this).val();
            $.ajax({
                url: `/Admin/Dashboard/GetTrendingMovies?filter=${selectedOption}`,
                method: "GET",
                success: function (response) {
                    var trendingMovies = $(".trending-movies");
                    console.log(response.data.length);
                    if (response.data.length == 0) {
                        trendingMovies.empty();
                        trendingMovies.append(`<div class="text-primary">
                                            No tickets sold for this time period.
                                             </div>`);
                    } else {                      
                        trendingMovies.empty(); // Clear existing content
                        $.each(response.data, function (index, data) {
                            var movieName = data.movie;
                            var totalRevenue = data.totalRevenue;
                            trendingMovies.append(`<div class="d-flex align-items-center border-bottom py-3">                  
                            <div class="w-100 ms-3">
                                <div class="d-flex w-100 justify-content-between">
                                    <h5 class="mb-0">${movieName}</h5>
                                    <h6 class="pb-0">Revenue: $${totalRevenue}</h6>
                                </div>
                            </div>
                        </div >`);
                        });
                    }

                }
            })

        });
    });
    // Single Bar Chart
    var ctx4 = $("#bar-chart").get(0).getContext("2d");
    var myChart4 = new Chart(ctx4, {
        type: "bar",
        data: {
            labels: ["Italy", "France", "Spain", "USA", "Argentina"],
            datasets: [{
                backgroundColor: [
                    "rgba(0, 156, 255, .7)",
                    "rgba(0, 156, 255, .6)",
                    "rgba(0, 156, 255, .5)",
                    "rgba(0, 156, 255, .4)",
                    "rgba(0, 156, 255, .3)"
                ],
                data: [55, 49, 44, 24, 15]
            }]
        },
        options: {
            responsive: true
        }
    });


    // Pie Chart
    var ctx5 = $("#pie-chart").get(0).getContext("2d");
    var myChart5 = new Chart(ctx5, {
        type: "pie",
        data: {
            labels: ["Italy", "France", "Spain", "USA", "Argentina"],
            datasets: [{
                backgroundColor: [
                    "rgba(0, 156, 255, .7)",
                    "rgba(0, 156, 255, .6)",
                    "rgba(0, 156, 255, .5)",
                    "rgba(0, 156, 255, .4)",
                    "rgba(0, 156, 255, .3)"
                ],
                data: [55, 49, 44, 24, 15]
            }]
        },
        options: {
            responsive: true
        }
    });


    // Doughnut Chart
    var ctx6 = $("#doughnut-chart").get(0).getContext("2d");
    var myChart6 = new Chart(ctx6, {
        type: "doughnut",
        data: {
            labels: ["Italy", "France", "Spain", "USA", "Argentina"],
            datasets: [{
                backgroundColor: [
                    "rgba(0, 156, 255, .7)",
                    "rgba(0, 156, 255, .6)",
                    "rgba(0, 156, 255, .5)",
                    "rgba(0, 156, 255, .4)",
                    "rgba(0, 156, 255, .3)"
                ],
                data: [55, 49, 44, 24, 15]
            }]
        },
        options: {
            responsive: true
        }
    });

})(jQuery);

