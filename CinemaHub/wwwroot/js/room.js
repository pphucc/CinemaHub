// Loading DataTable with Ajax
var dataTable;
$(document).ready(function () {
    loadDataTable();
})
function loadDataTable() {
    dataTable = $('#roomTable').DataTable({
        "ajax": {
            "url": "/CinemaManager/Room/GetAllRooms"
        },
        "columns": [
            //{ "data": "roomID", "width": "20%" },
            { "data": "roomName", "width": "10%" },
            { "data": "status", "width": "10%" },
            { "data": "cinema.cinemaName", "width": "25%" },
            { "data": "note", "width": "20%" },
            { "data": "numOfSeats", "width": "5%" },

            {
                "data": "roomID",
                "render": function (data) {
                    return `
                       
                            <a href="/CinemaManager/Room/Update?room_id=${data}"
                            class="btn btn-dark mx-2"> <i class="bi bi-pencil-square"></i> Edit</a>
                            <a onClick=Delete('/CinemaManager/Room/Delete?room_id=${data}')
                            class="btn btn-danger mx-2"> <i class="bi bi-trash-fill"></i> Delete</a>
					        <a href="/CinemaManager/Seat/RoomSeats?room_id=${data}"
                            class="btn btn-secondary mt-2 mx-4"> <i class="bi bi-pencil-square"></i> Manage Seats</a>
                        `
                },
                "width": "15%"
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