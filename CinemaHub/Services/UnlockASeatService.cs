using Microsoft.EntityFrameworkCore;
using CinemaHub.DataAccess.Data;
using CinemaHub.DataAccess.Repositories;
using CinemaHub.Models;

namespace CinemaHub.Services
{
    public class UnlockASeatService : IUnlockASeatService
    {
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly IUnitOfWork _unitOfWork;
        private readonly object _lockObject = new object();
        public UnlockASeatService(IDbContextFactory<AppDbContext> dbContextFactory, IUnitOfWork unitOfWork)
        {
            _dbContextFactory = dbContextFactory;
            _unitOfWork = unitOfWork;
        }
        public void UnlockASeat(Guid seat_id, Guid showtime_id, string? status)
        {
            lock (_lockObject)
            {
                try
                {
                    Seat seat = _unitOfWork.Seat.GetFirstOrDefaultAsync(u => u.SeatID == seat_id).Result;
                    if (seat.SeatStatus.ToLower().Contains((showtime_id.ToString() + "_status=" + status).ToLower()))
                    {
                        seat.SeatStatus = seat.SeatStatus.Replace((showtime_id.ToString() + "_status=" + status), "");
                        if (seat.SeatStatus.ToLower() == "locked_")
                        {
                            seat.SeatStatus = "AVAILABLE";
                        }
                    }
         
                    _unitOfWork.Seat.Update(seat);
                    _unitOfWork.Save();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    throw;
                }
            }

        }
    }
}
