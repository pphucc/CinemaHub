// Loading DataTable with Ajax
var dataTable;
$(document).ready(function () {
    loadDataTable();
})
function loadDataTable() {
    dataTable = $('#movieTable').DataTable({
        "ajax": {
            "url": "/CinemaManager/Movie/GetAllMovies"
        },
        "columns": [
            { "data": "movieName", "width": "10%" },
            {
                "data": "imageUrl",
                "render": function (data) {
                    return `<td class="image-column" style="height:150px">
                                <img src="${data}" alt="Alternate Text" class=" img-fluid image-element" />
                            </td>`;
                },
                "width": "15%"
            },
            {
                "data": "videoUrl",
                "render": function (data) {
                    return `<td class="video-column" style="height:150px">
                                <video src="${data}" type="video/mp4" class="video-element" controls style="width:100%; height:100%"></video>
                            </td>`;
                },
                "width": "15%"
            },
            { "data": "releaseDate", "width": "10%" },
            { "data": "endDate", "width": "10%" },
            {
                "data": "movieID",
                "render": function (data) {
                    return `<td class="button-column">
                                <a href="/CinemaManager/Movie/Update?movie_id=${data}" class="btn btn-dark mx-1">
                                    <i class="bi bi-pencil-square"></i> Edit
                                </a>
                                <a onClick=Delete('/CinemaManager/Movie/Delete?movie_id=${data}') class="btn btn-danger mx-1">
                                    <i class="bi bi-trash-fill"></i> Delete
                                </a>
                            </td>`;
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
                            'This movie has been deleted.',
                            'success'
                        )
                    } else {
                        // Handle error case
                    }
                }
            })
        }
    })
}