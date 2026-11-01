# LearnHub LMS — Database Schema Design

## 1. Database Overview

**Database:** SQL Server  
**ORM:** Entity Framework Core  
**Design approach:** Relational database with normalized tables.

The system will use a shared `Users` table for authentication and role management, with separate profile tables for students and instructors.

## 2. Database Tables

### 2.1 Users

The central table for all system accounts.

| Column       | Type             | Constraints         | Description                |
| ------------ | ---------------- | ------------------- | -------------------------- |
| Id           | uniqueidentifier | PK                  | User identifier            |
| Name         | nvarchar(100)    | NOT NULL            | Full name                  |
| Email        | nvarchar(255)    | UNIQUE, NOT NULL    | Login email                |
| PasswordHash | nvarchar(255)    | NOT NULL            | Hashed password            |
| Role         | int              | NOT NULL            | ADMIN, STUDENT, INSTRUCTOR |
| IsActive     | bit              | NOT NULL, DEFAULT 1 | Account activation status  |
| TokenVersion | int              | NOT NULL, DEFAULT 0 | Token invalidation version |
| CreatedAt    | datetime2        | NOT NULL            | Account creation date      |
| UpdatedAt    | datetime2        | NULL                | Last update date           |

**Responsibilities:**

- Store authentication credentials.
- Identify the user's role.
- Control account activation.
- Support role management.
- Support token invalidation when roles or account status change.

### 2.2 StudentProfiles

Stores student-specific information.

| Column    | Type             | Constraints | Description           |
| --------- | ---------------- | ----------- | --------------------- |
| UserId    | uniqueidentifier | PK, FK      | References Users.Id   |
| CreatedAt | datetime2        | NOT NULL    | Profile creation date |
| UpdatedAt | datetime2        | NULL        | Last update date      |

A student profile is created when a user registers as a student.

### 2.3 InstructorProfiles

Stores instructor-specific information and verification details.

| Column             | Type             | Constraints | Description                         |
| ------------------ | ---------------- | ----------- | ----------------------------------- |
| UserId             | uniqueidentifier | PK, FK      | References Users.Id                 |
| VerificationStatus | int              | NOT NULL    | PENDING, VERIFIED, REJECTED         |
| ReviewedBy         | uniqueidentifier | NULL, FK    | Admin who reviewed the registration |
| ReviewedAt         | datetime2        | NULL        | Verification review date            |
| RejectionReason    | nvarchar(500)    | NULL        | Reason for rejection                |
| CreatedAt          | datetime2        | NOT NULL    | Profile creation date               |
| UpdatedAt          | datetime2        | NULL        | Last update date                    |

**Responsibilities:**

- Store instructor verification status.
- Track which administrator reviewed the registration.
- Record when verification occurred.
- Store rejection reasons when applicable.

Default verification status: `PENDING`.

### 2.4 InstructorVerificationHistory

Stores the history of instructor verification decisions.

| Column         | Type             | Constraints  | Description                          |
| -------------- | ---------------- | ------------ | ------------------------------------ |
| Id             | uniqueidentifier | PK           | Record identifier                    |
| InstructorId   | uniqueidentifier | FK, NOT NULL | References InstructorProfiles.UserId |
| AdminId        | uniqueidentifier | FK, NOT NULL | References Users.Id                  |
| PreviousStatus | int              | NULL         | Previous verification status         |
| NewStatus      | int              | NOT NULL     | New verification status              |
| Reason         | nvarchar(500)    | NULL         | Review reason                        |
| CreatedAt      | datetime2        | NOT NULL     | Decision date                        |

This table provides an audit trail whenever an administrator approves or rejects an instructor.

---

## 3. Enums

### UserRole

| Value | Name       |
| ----- | ---------- |
| 1     | ADMIN      |
| 2     | STUDENT    |
| 3     | INSTRUCTOR |

### InstructorVerificationStatus

| Value | Name     |
| ----- | -------- |
| 1     | PENDING  |
| 2     | VERIFIED |
| 3     | REJECTED |

---

## 4. Table Relationships

- Users → StudentProfiles: One-to-One (optional).
- Users → InstructorProfiles: One-to-One (optional).
- InstructorProfiles → InstructorVerificationHistory: One-to-Many.
- Users → InstructorVerificationHistory (AdminId): One-to-Many.

A user has exactly one current role.

Only users with the `STUDENT` role should have a student profile, and only users with the `INSTRUCTOR` role should have an instructor profile.

Administrators do not need a separate profile table because their information is already stored in Users.

---

## 5. Important Database Rules

1. Email addresses must be unique.
2. Passwords must always be stored as secure hashes.
3. Public registration cannot create ADMIN accounts.
4. Student registration creates a StudentProfile.
5. Instructor registration creates an InstructorProfile with PENDING status.
6. Only administrators can change user roles.
7. Role changes must increment TokenVersion to invalidate existing tokens.
8. Deactivating an account must not physically delete its records.
9. An administrator cannot deactivate the last active administrator.
10. Instructor verification decisions must be recorded in InstructorVerificationHistory.
11. Only active administrators can review instructor registrations.
12. A pending or rejected instructor cannot access protected instructor functionality.

---

## 6. Indexes and Constraints

| Table                         | Index / Constraint          |
| ----------------------------- | --------------------------- |
| Users                         | Unique index on Email       |
| Users                         | Index on Role               |
| Users                         | Index on IsActive           |
| InstructorProfiles            | Index on VerificationStatus |
| InstructorProfiles            | FK on ReviewedBy            |
| InstructorVerificationHistory | Index on InstructorId       |
| InstructorVerificationHistory | Index on AdminId            |
| StudentProfiles               | FK on UserId                |
| InstructorProfiles            | FK on UserId                |

---

## 7. Design Decisions

**Why one Users table?**

Authentication is shared between administrators, students, and instructors. Keeping credentials in one table avoids duplicating authentication logic and makes role management straightforward.

**Why separate profile tables?**

Students and instructors have different responsibilities and information. Separate tables allow us to extend their profiles without adding unnecessary nullable columns to Users.

**Why soft deactivation instead of deletion?**

Account history and verification records should remain available for administrative purposes.

**Why TokenVersion?**

When an administrator changes a user's role or deactivates an account, existing tokens must no longer authorize requests using outdated permissions.

**Why InstructorVerificationHistory?**

The current verification status alone cannot tell us who approved or rejected an instructor previously. Keeping a history provides accountability.

---

## 8. Initial Administrator

The first administrator will be created through a controlled developer setup process.

Public registration will never allow ADMIN role assignment.

Additional administrators can be created through the administrator management functionality.
