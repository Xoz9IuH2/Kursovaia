using BCryptNet = BCrypt.Net.BCrypt;
using web_api.Data;
using web_api.Models;

namespace web_api.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(VoenkomDbContext context)
    {
        context.Database.EnsureCreated();

        if (context.Users.Any())
        {
            return; // БД уже инициализирована
        }

        var personalFile = new PersonalFile
        {
            Status = "active",
            CreatedAt = DateTime.UtcNow
        };
        context.PersonalFiles.Add(personalFile);
        await context.SaveChangesAsync();

        var users = new List<User>
        {
            new User
            {
                Login = "employee1@voenkom.ru",
                PasswordHash = BCryptNet.HashPassword("Employee1"),
                Role = "employee",
                Name = "Иванов Иван Иванович",
                Email = "employee1@voenkom.ru",
                Phone = "+79001234567",
                MustChangePassword = false,
                DateOfBirth = new DateTime(1975, 5, 15),
                RegistrationAddress = "г. Москва, ул. Арбат, д.10",
                ResidenceAddress = "г. Москва, ул. Арбат, д.10",
                Passport_series = "4512",
                Passport_number = "123456",
                Passport_issued = "ОВД района Арбат",
                Passport_date = new DateTime(2015, 3, 15),
                MilitaryTicketNumber = "АБ №123456",
                FitnessCategory = "А",
                AccountStatus = "Состоит на учёте"
            },
            new User
            {
                Login = "employee2@voenkom.ru",
                PasswordHash = BCryptNet.HashPassword("Employee2"),
                Role = "employee",
                Name = "Петров Петр Петрович",
                Email = "employee2@voenkom.ru",
                Phone = "+79007654321",
                MustChangePassword = true,
                DateOfBirth = new DateTime(1985, 8, 20),
                RegistrationAddress = "г. Москва, ул. Ленина, д.5",
                FitnessCategory = "Б-1",
                AccountStatus = "Состоит на учёте",
                PersonalFileId = personalFile.Id
            },
            new User
            {
                Login = "citizen1@mail.ru",
                PasswordHash = BCryptNet.HashPassword("Citizen1"),
                Role = "citizen",
                Name = "Алексеев Александр Дмитриевич",
                Email = "citizen1@mail.ru",
                Phone = "+79009876543",
                MustChangePassword = false,
                DateOfBirth = new DateTime(2002, 3, 15),
                RegistrationAddress = "г. Москва, ул. Ленина, д.5, кв.12",
                ResidenceAddress = "г. Москва, ул. Ленина, д.5, кв.12",
                Passport_series = "4512",
                Passport_number = "234567",
                Passport_issued = "ОВД района ЦАО",
                Passport_date = new DateTime(2019, 4, 10),
                MilitaryTicketNumber = "АВ №654321",
                FitnessCategory = "А",
                AccountStatus = "Состоит на учёте",
                PersonalFileId = personalFile.Id
            }
        };

        context.Users.AddRange(users);
        await context.SaveChangesAsync();

        var notifications = new List<Notification>
        {
            new Notification
            {
                UserId = users[2].Id,
                Title = "🪪 Новая повестка",
                Message = "Вам назначена явка на медкомиссию 15.05.2026 в 10:00",
                Type = "summon",
                Action = "summon:1",
                CreatedAt = DateTime.UtcNow.AddHours(-2)
            },
            new Notification
            {
                UserId = users[2].Id,
                Title = "📄 Статус заявления",
                Message = "Ваше заявление на отсрочку переведено в статус \"Принято\"",
                Type = "application",
                Action = "application:1",
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            },
            new Notification
            {
                UserId = users[2].Id,
                Title = "⏰ Напоминание",
                Message = "Напоминаем о явке завтра, 15.05.2026",
                Type = "alert",
                Action = "summon:1",
                CreatedAt = DateTime.UtcNow.AddHours(-1)
            }
        };

        context.Notifications.AddRange(notifications);
        await context.SaveChangesAsync();

        var summons = new List<Summon>
        {
            new Summon
            {
                PersonalFileId = personalFile.Id,
                Title = "Медкомиссия",
                Description = "Прохождение медицинской комиссии",
                SummonDate = new DateTime(2026, 5, 15),
                Time = "10:00",
                Location = "г. Москва, ул. Арбат, д.10, каб. 205",
                Status = "delivered",
                CreatedAt = DateTime.UtcNow.AddDays(-3)
            }
        };

        context.Summons.AddRange(summons);
        await context.SaveChangesAsync();

        var calendarEvents = new List<CalendarEvent>
        {
            new CalendarEvent
            {
                Title = "Медкомиссия",
                Description = "Прохождение медицинской комиссии",
                EventDate = new DateTime(2026, 5, 15),
                StartTime = "09:00",
                EndTime = "12:00",
                Location = "г. Москва, ул. Арбат, д.10",
                EventType = "medical",
                CreatedById = users[0].Id,
                IsAvailable = true,
                MaxSlots = 20,
                BookedSlots = 5
            },
            new CalendarEvent
            {
                Title = "Призывная комиссия",
                Description = "Заседание призывной комиссии",
                EventDate = new DateTime(2026, 6, 1),
                StartTime = "10:00",
                EndTime = "14:00",
                Location = "г. Москва, ул. Арбат, д.10",
                EventType = "commission",
                CreatedById = users[0].Id,
                IsAvailable = true,
                MaxSlots = 30,
                BookedSlots = 12
            }
        };

        context.CalendarEvents.AddRange(calendarEvents);
        await context.SaveChangesAsync();
    }
}
