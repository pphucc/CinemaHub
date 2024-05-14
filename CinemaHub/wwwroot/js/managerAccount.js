// Loading DataTable with Ajax
var dataTable;
$(document).ready(function () {
    loadDataTable();
})
function loadDataTable() {
    dataTable = $('#managerAccountTable').DataTable({
        "ajax": {
            "url": "/Admin/Dashboard/GetManagerUsers"
        },
        "columns": [
            { "data": "id", "width": "5%" },
            { "data": "firstName", "width": "10%" },
            { "data": "lastName", "width": "10%" },
            { "data": "email", "width": "10%" },
            { "data": "phoneNumber", "width": "10%" },
            {
                "data": "lockoutEnd", render: (data) => {
                    if (data != null) {
                        const lockoutEndDate = new Date(data);
                        const currentDate = new Date();
                        if (lockoutEndDate >= currentDate) {
                            return `Locked!`
                        }
                    }                    
                    return `OK`
            }, "width": "10%" },
            {
                "data": "id",
                "render": function (data) {
                    
                    return `  
                    <div class="d-flex justity-content-between align-items-center w-100">
                            <a asp-area="Admin" onClick=LockAccount("/Admin/Dashboard/LockAccount?user_id=${data}")
                            class="btn btn-danger mx-2"><i class="bi bi-lock"></i>Lock</a>
                            <a asp-area="Admin" onClick=UnlockAccount("/Admin/Dashboard/UnlockAccount?user_id=${data}")
                            class="btn btn-primary mx-2"><i class="bi bi-unlock-fill"></i>Unlock</a>
                            <a href="/Admin/Dashboard/UpdateManagerAccount?user_id=${data}")
                            class="btn btn-primary mx-2"><i class="bi bi-pencil-square"></i></i>Edit</a>
					 </div>                         `
                },
                "width": "20%"
            }
        ]
    });
}

// SweetAlert library
function LockAccount(url) {
    Swal.fire({
        title: 'Are you sure?',
        text: "Do you want to lock this user",
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#3085d6',
        cancelButtonColor: '#d33',
        confirmButtonText: 'Yes, lock it!'
    }).then((result) => {
        if (result.isConfirmed) {
            $.ajax({
                url: url,
                type: 'POST',
                success: function (data) {
                    if (data.success) {
                        dataTable.ajax.reload();
                        Swal.fire(
                            'Locked!',
                            'This account has been locked!',
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
function UnlockAccount(url) {
    Swal.fire({
        title: 'Are you sure?',
        text: "Do you want to unlock this user",
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#3085d6',
        cancelButtonColor: '#d33',
        confirmButtonText: 'Yes, unlock it!'
    }).then((result) => {
        if (result.isConfirmed) {
            $.ajax({
                url: url,
                type: 'POST',
                success: function (data) {
                    if (data.success) {
                        dataTable.ajax.reload();
                        Swal.fire(
                            'Locked!',
                            'This account has been unlocked!',
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