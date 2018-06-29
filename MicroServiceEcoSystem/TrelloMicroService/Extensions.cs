using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrelloMicroService
{
    using Manatee.Trello;

    /// <summary>   An extensions. </summary>
    public static class Extensions
    {
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   A DateTime extension method that truncates. </summary>
        ///
        /// <param name="dateTime"> The dateTime to act on. </param>
        /// <param name="timeSpan"> The time span. </param>
        ///
        /// <returns>   A DateTime. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public static DateTime Truncate(this DateTime dateTime, TimeSpan timeSpan)
        {
            if (timeSpan == TimeSpan.Zero)
                return dateTime; // Or could throw an ArgumentException
            return dateTime.AddTicks(-(dateTime.Ticks % timeSpan.Ticks));
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   An IBoard extension method that ensures that power up. </summary>
        ///
        /// <param name="board">    The board to act on. </param>
        /// <param name="powerUp">  The power up. </param>
        ///
        /// <returns>   The asynchronous result. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public static async Task EnsurePowerUp(this IBoard board, IPowerUp powerUp)
        {
            if (board.PowerUps.Any(p => p?.Id == powerUp?.Id)) 
                return;

            await board.PowerUps.EnablePowerUp(powerUp);
            await board.PowerUps.Refresh();
        }
    }
}
