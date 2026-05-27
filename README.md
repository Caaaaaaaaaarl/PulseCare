# 💊 PulseCare

> A web-based healthcare management system built with ASP.NET Core MVC.  
> PulseCare allows patients and doctors to manage appointments, consultations, and reminders.

---

## 👥 Developers — BSIT 3A

| Name |
|------|
| John Carl Q. Bolo |
| Adrian Kim P. De Guzman |
| Carlo V. Delos Santos |

---

## 🛠️ Built With

| Technology | Purpose |
|------------|---------|
| ASP.NET Core MVC (.NET 8.0) | Web framework |
| Entity Framework Core | Database ORM |
| SQL Server | Database |
| C# | Backend logic |
| HTML / CSS / JavaScript | Frontend views |

---

## 📁 Project Structure

````
PulseCare/
├── Controllers/
│   ├── AccountController.cs
│   ├── AdminController.cs
│   ├── AppointmentController.cs
│   ├── ConsultationController.cs
│   ├── HomeController.cs
│   ├── ReminderController.cs
│   └── SettingsController.cs
├── Data/
│   └── ApplicationDbContext.cs
├── Migrations/
├── Models/
│   ├── Appointment.cs
│   ├── Consultation.cs
│   ├── Reminder.cs
│   └── User.cs
├── Views/
│   ├── Account/
│   ├── Admin/
│   ├── Appointment/
│   ├── Consultation/
│   ├── Home/
│   ├── Reminder/
│   ├── Settings/
│   └── Shared/
├── wwwroot/
├── appsettings.json
└── Program.cs
````

---

## ✨ Features

| Feature | Description |
|---------|-------------|
| 🔐 User Authentication | Role-based login for Patient, Doctor, and Admin |
| 📅 Appointments | Book, view, and manage appointments |
| 🩺 Consultations | Record consultations with fee tracking |
| 🔔 Reminders | Set and manage health reminders |
| ⚙️ Settings | User account settings and preferences |
| 🛡️ Admin Dashboard | Manage users and system data |

---

## 🚀 Getting Started

**1. Clone the repository**
```bash
git clone https://github.com/Caaaaaaaaaarl/PulseCare.git
cd PulseCare/PulseCare
```

**2. Update the connection string in `appsettings.json`**
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=YOUR_SERVER;Database=PulseCareDb;Trusted_Connection=True;"
}
```

**3. Apply database migrations**
```bash
dotnet ef database update
```

**4. Run the application**
```bash
dotnet run
```

---

## 🧪 Test Accounts

| Role | Email | Password |
|------|-------|----------|
| 🛡️ Admin | admin@pulsecare.com | 123 |
| 🧑‍⚕️ Patient | patient@pulsecare.com | 123 |
| 👨‍⚕️ Doctor | doctor@pulsecare.com | 123 |

---

## 📄 License

This project is for **educational purposes** only.

© 2026 PulseCare — BSIT 3A
