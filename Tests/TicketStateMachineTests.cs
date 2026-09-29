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
                TicketStatus = TicketStatus.Closed,
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
        public void Assigned_icin_atanan_zorunlu()
        {
            var ticket = new Ticket
            {
                TicketStatus = TicketStatus.New,
                AssignedUserId = null,
                Title = "x",
                Description = "x",
                CustomerName = "Ali",
                CustomerEmail = "ali@ornek.com",
                TicketNumber = "REQ-2026-00001"
            };

            var ex = Assert.Throws<InvalidOperationException>(
                () => TicketStateMachine.EnsureTransition(ticket, TicketStatus.Assigned));

            Assert.Contains("atanmış", ex.Message);
        }

        [Fact]
        public void Atanan_varsa_New_den_Assigned_gecerlidir()
        {
            var ticket = new Ticket
            {
                TicketStatus = TicketStatus.New,
                AssignedUserId = "emp-1",
                Title = "x",
                Description = "x",
                CustomerName = "Ali",
                CustomerEmail = "ali@ornek.com",
                TicketNumber = "REQ-2026-00001"
            };

            TicketStateMachine.EnsureTransition(ticket, TicketStatus.Assigned);
        }
    }
}
