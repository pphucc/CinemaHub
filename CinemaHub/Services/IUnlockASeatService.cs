using CinemaHub.Models;

namespace CinemaHub.Services
{
	public interface IUnlockASeatService
	{
		void UnlockASeat(Guid seat_id, Guid showtime_id, string? status);
		
	}
}
