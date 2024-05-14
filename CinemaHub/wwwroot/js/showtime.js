
var dataTable;

$(document).ready(function () {
    loadDataTable();
})
function loadDataTable() {
    dataTable = $('#showtimeTable').DataTable({
        "ajax": {
            "url": "/CinemaManager/Showtime/GetAllShowtime"
        },
        "columns": [
            //{ "data": "showtimeID", "width": "20%" },
            { "data": "date", "width": "15%" },
            {
                "data": "time",
                "width": "10%",
                "render": function (data) {
                    return data + "h";
                }
            },
            {
                "data": "minute",
                "width": "10%",
                "render": function (data) {
                    return data + "m";
                }
            },
            { "data": "movie.movieName", "width": "10%" },
            { "data": "movie.duration", "width": "10%" },
            { "data": "cinema_name", "width": "15%" },
            { "data": "room.roomName", "width": "10%" },

            {
                "data": "showtimeID",
                "render": function (data) {
                    return `
                       
                            <a href="/CinemaManager/Showtime/Update?showtime_id=${data}"
                            class="btn btn-dark mx-2"> <i class="bi bi-pencil-square"></i> Edit</a>
                            <a onClick=Delete('/CinemaManager/Showtime/Delete?showtime_id=${data}')
                            class="btn btn-danger mx-2"> <i class="bi bi-trash-fill"></i> Delete</a>
					   
                        `
                },
                "width": "20%"
            }
        ]
    });
}


// SweetAlert library
function Delete(url) {
    Swal.fire({
        title: 'Are you sure?',
        text: "You won't be able to revert this!",
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#3085d6',
        cancelButtonColor: '#d33',
        confirmButtonText: 'Yes, delete it!'
    }).then((result) => {
        if (result.isConfirmed) {
            $.ajax({
                url: url,
                type: 'DELETE',
                success: function (data) {
                    if (data.success) {
                        dataTable.ajax.reload();
                        Swal.fire(
                            'Deleted!',
                            'This room has been deleted.',
                            'success'
                        )
                        //toastr.success(data.message);
                    } else {
                        //toastr.error(data.message);
                    }
                }
            })
        }
    })
}