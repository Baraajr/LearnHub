# LearnHub LMS — Feature List

## 1. Administrator Features

### F-01: Admin Login

**Endpoint:** `POST /api/auth/login`

- Allow administrators to log in using their credentials.
- Validate credentials and identify the user's role.
- Display an appropriate error for invalid credentials.
- Grant access to admin-only functionality.
- Prevent users with other roles from accessing administrator functionality.
- Create the initial administrator through a controlled developer setup process.
- Allow existing administrators to create additional administrator accounts.

---

### F-02: Admin Home Page

**Endpoint:** `GET /api/admin/dashboard`

- Display the administrator dashboard after successful login.
- Provide navigation to admin, student, and instructor management.
- Display summary counts of registered administrators, students, and instructors.
- Provide access to instructor verification requests.

---

### F-03: Manage Admin List

- View the list of administrators.
- View an individual administrator's details.
- Create new administrator accounts.
- Edit administrator information.
- Delete or deactivate administrator accounts, subject to safeguards.
- Prevent deletion or deactivation of the last active administrator.

| Method   | Endpoint          | Description                                   |
| -------- | ----------------- | --------------------------------------------- |
| `GET`    | `/api/admins`     | Get all administrators                        |
| `GET`    | `/api/admins/:id` | Get administrator details                     |
| `POST`   | `/api/admins`     | Create an administrator account               |
| `PATCH`  | `/api/admins/:id` | Update administrator information              |
| `DELETE` | `/api/admins/:id` | Delete or deactivate an administrator account |

---

### F-04: Manage Student List

- View all registered students.
- View individual student information.
- Edit student information.
- Delete or deactivate student accounts.
- Ensure only authorized administrators can manage student accounts.

| Method   | Endpoint            | Description                            |
| -------- | ------------------- | -------------------------------------- |
| `GET`    | `/api/students`     | Get all students                       |
| `GET`    | `/api/students/:id` | Get student details                    |
| `PATCH`  | `/api/students/:id` | Update student information             |
| `DELETE` | `/api/students/:id` | Delete or deactivate a student account |

---

### F-05: Manage Instructor List

- View all registered instructors.
- View individual instructor information.
- Edit instructor information.
- Delete or deactivate instructor accounts.
- View the verification status of each instructor.
- Ensure only authorized administrators can manage instructor accounts.

| Method   | Endpoint               | Description                                |
| -------- | ---------------------- | ------------------------------------------ |
| `GET`    | `/api/instructors`     | Get all instructors                        |
| `GET`    | `/api/instructors/:id` | Get instructor details                     |
| `PATCH`  | `/api/instructors/:id` | Update instructor information              |
| `DELETE` | `/api/instructors/:id` | Delete or deactivate an instructor account |

---

### F-06: Manage User Roles

**Endpoint:** `PATCH /api/users/:userId/role`

- Allow administrators to change a user's role between `ADMIN`, `STUDENT`, and `INSTRUCTOR`.
- Prevent users from changing their own roles.
- Validate role changes on the backend.
- Ensure role changes take effect for existing sessions or tokens.
- Prevent deletion or deactivation of the last active administrator.
- Ensure role changes do not bypass instructor verification requirements.

**Example request body:**

```json
{
  "role": "INSTRUCTOR"
}
```

---

### F-07: Verify Instructor Registration

**Endpoint:** `PATCH /api/instructors/:id/status`

- Allow administrators to view instructors awaiting verification.
- Allow administrators to approve instructor registrations.
- Allow administrators to reject instructor registrations.
- Store each instructor's verification status.
- Restrict pending or rejected instructors from protected instructor functionality.
- Allow verified instructors to access authorized instructor functionality.

**Instructor statuses:**

| Status     | Meaning                                          |
| ---------- | ------------------------------------------------ |
| `PENDING`  | Registered, awaiting administrator review        |
| `VERIFIED` | Approved; has access to instructor functionality |
| `REJECTED` | Declined; no access to instructor functionality  |

**Example request body:**

```json
{
  "status": "VERIFIED"
}
```

---

## 2. Student Features

### F-08: Student Registration

**Endpoint:** `POST /api/auth/register`

- Allow new users to register as students.
- Collect the required registration information.
- Validate submitted information.
- Create a student account with the `STUDENT` role.
- Prevent public registration from creating administrator accounts.
- Prevent duplicate accounts using an already registered email address.

**Example request body:**

```json
{
  "name": "John Doe",
  "email": "john@example.com",
  "password": "StrongPassword123",
  "role": "STUDENT"
}
```

---

### F-09: Student Login

**Endpoint:** `POST /api/auth/login`

- Allow registered students to log in using their credentials.
- Validate credentials.
- Confirm that the account is active and has the `STUDENT` role.
- Grant access to student-specific functionality.
- Display an appropriate error for invalid credentials or unauthorized access.

---

### F-10: Student Home Page

**Endpoint:** `GET /api/students/me`

- Display the student home page after successful login.
- Display basic information about the logged-in student.
- Provide navigation to functionality available within the current project scope.
- Restrict access to authenticated students.
- Prevent students from accessing administrator or instructor-only functionality.

---

## 3. Instructor Features

### F-11: Instructor Registration

**Endpoint:** `POST /api/auth/register`

- Allow new users to register as instructors.
- Collect the required registration information.
- Validate submitted information.
- Create an instructor account with the `INSTRUCTOR` role.
- Set the instructor's initial verification status to `PENDING`.
- Prevent public registration from creating administrator accounts.
- Prevent duplicate accounts using an already registered email address.
- Inform the instructor that administrator verification is required before accessing protected instructor functionality.

**Example request body:**

```json
{
  "name": "Jane Smith",
  "email": "jane@example.com",
  "password": "StrongPassword123",
  "role": "INSTRUCTOR"
}
```

---

### F-12: Instructor Login

**Endpoint:** `POST /api/auth/login`

- Allow registered instructors to log in using their credentials.
- Validate credentials and confirm the user's role.
- Ensure the account is active.
- Restrict pending or rejected instructors from protected instructor functionality.
- Grant verified instructors access to authorized instructor functionality.
- Display an appropriate error or status message when verification is pending or rejected.

---

### F-13: Instructor Home Page

**Endpoint:** `GET /api/instructors/me`

- Display the instructor home page after successful login.
- Display basic information about the logged-in instructor.
- Display the instructor's verification status when applicable.
- Provide navigation to functionality available within the current project scope.
- Restrict protected instructor functionality to verified instructors.
- Prevent instructors from accessing administrator-only functionality.
