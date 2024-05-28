using CinemaHub.Models;

namespace CinemaHub.Services.IServices
{
    public interface IUnlockASeatService
    {
        void UnlockASeat(Guid seat_id, Guid showtime_id, string? status);
        void LockASeat(Seat seat, Guid showtime_id, string? status);
    }
}
