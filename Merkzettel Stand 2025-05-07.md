DOKUMENTATION OF USERFLOW API 

🧠 Entwickler-Merkzettel – UserFlow API (Stand: 2025-05-07)
🔧 Projektstruktur & Hauptfeatures

    .NET 8 WebAPI mit PostgreSQL & Entity Framework Core

    Entities: User, Company, Project, Screen, ScreenAction, Note, RefreshToken, Employee, ScreenActionType

    DTOs + Mapping: Modular pro Entity, ToXyzDto()-Methoden via Mapper-Klassen mit Expressions

    Authentication & Authorization:

        Identity mit JWT + Refresh Tokens

        Benutzeraktivierung über Passwortvergabe (NeedsPasswordSetup, IsActive)

        Rollen: GlobalAdmin, Admin, User, Manager

    Soft Delete + Multi-Tenancy: über globale QueryFilter (IsDeleted, CompanyId, UserId)

    Import/Export: per CSV (CsvHelper), inkl. Fehlerbehandlung

    Bulk Operationen: über BulkOperationResultDTO<T> mit BulkOperationErrorDTO

    Logging: systematisch mit strukturierten, emoji-basierten Logeinträgen

    API-Konventionen:

        Alle [Route]-Attribute explizit gesetzt (api/[controller] oder api/xyz)

        API-Endpunkte verwenden sinnvolle Rollen-Absicherung via [Authorize(Roles = "...")]

📦 Letzte Änderungen

    🧹 Entity Cleanup:

        Project.ProjectId & Company.CompanyId entfernt → BaseEntity.Id wird verwendet

    🔄 Projektfreigabe:

        IsShared-Flag bei Project hinzugefügt und in Queries berücksichtigt

    🔐 Registrierungskonzept:

        Admin kann Benutzer registrieren → Benutzer aktiviert sich später durch Passwort

        RegisterDTO, SetPasswordDTO, CompleteRegistrationDTO

    🧪 Tests & DataSeeder:

        Zufälliges Setzen von IsShared zur Validierung in Seeding

    ✅ Statussteuerung Login:

        IsActive blockiert Login, solange Passwort nicht gesetzt wurde

📝 Kommentier-Richtlinien für Doxygen

    ⚠️ Code darf NIE verändert werden – nur Kommentare!

    Doxygen-Header über jeder Datei:

/// @file AuthController.cs
/// @author ...
/// @date ...
/// @brief ...
/// @details ...

Zusätzlicher Abschnitt bei Controllern:

/// @endpoints
/// - POST /api/auth/login → Login for users
/// - POST /api/auth/register → Admin registration of user

Jede Methode mit /// und Emoji kommentieren

So viele Codezeilen wie möglich kommentieren, z. B.:

var user = await _context.Users
    .IgnoreQueryFilters() // ⚠️ Ignoring soft-delete filter
    .FirstOrDefaultAsync(...); // 🔍 Load user by email

Regionen verwenden:

#region 🔐 Authorization
...
#endregion

Alle Texte in Englisch

Emojis in Kommentaren & Log-Messages verwenden (✅, ❌, 🚀, 🔐 etc.)