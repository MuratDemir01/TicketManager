using TicketManager.Entities;
using TicketManager.Enums;
using TicketManager.Services;

namespace TicketManager.Tests
{
    public class TicketStateMachineTests
    {
        [Fact]
        public void Closed_talep_durumu_degistirilemez()
        {
            var ticket = new Ticket
            {
                Id = 1,
                Status = TicketStatus.Closed,
                Title = "Kapalı talep",
                Description = "x",
                CustomerName = "Ali",
                CustomerEmail = "ali@ornek.com",
                TicketNumber = "REQ-2026-00001"
            };

            var ex = Assert.Throws<InvalidOperationException>(
                () => TicketStateMachine.EnsureCanModify(ticket));

            Assert.Contains("Closed", ex.Message);
        }

        [Fact]
        public void New_den_Closed_a_gecis_gecersizdir()
        {
            var ex = Assert.Throws<InvalidOperationException>(
                () => TicketStateMachine.EnsureTransition(TicketStatus.New, TicketStatus.Closed));

            Assert.Contains("Geçersiz durum geçişi", ex.Message);
        }

        [Fact]
        public void OnHold_den_InProgress_veya_Resolved_gecerlidir()
        {
            TicketStateMachine.EnsureTransition(TicketStatus.OnHold, TicketStatus.InProgress);
            TicketStateMachine.EnsureTransition(TicketStatus.OnHold, TicketStatus.Resolved);
        }

        [Fact]
        public void OnHold_den_Closed_gecersizdir()
        {
            var ex = Assert.Throws<InvalidOperationException>(
                () => TicketStateMachine.EnsureTransition(TicketStatus.OnHold, TicketStatus.Closed));

            Assert.Contains("Geçersiz durum geçişi", ex.Message);
        }
    }
}
