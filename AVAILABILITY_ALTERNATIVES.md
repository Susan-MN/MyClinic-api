# Alternative Approaches for Doctor Availability & Leave Management

## Current Implementation Issues

- `WorkingDaysJson` stored as string (hard to query/validate)
- Single schedule for all working days (can't have different hours per day)
- Time stored as strings instead of proper time types
- Limited flexibility for exceptions

---

## Option 1: **Day-Based Availability Table** (Recommended for Flexibility)

### Structure

Create a separate `AvailabilityDay` table where each row represents one day's schedule:

```sql
CREATE TABLE AvailabilityDays (
    Id INT PRIMARY KEY IDENTITY(1,1),
    DoctorId INT NOT NULL,
    DayOfWeek INT NOT NULL, -- 0=Sunday, 1=Monday, ..., 6=Saturday
    StartTime TIME NOT NULL,
    EndTime TIME NOT NULL,
    SlotDuration INT NOT NULL, -- in minutes
    IsActive BIT NOT NULL DEFAULT 1,
    FOREIGN KEY (DoctorId) REFERENCES Doctors(Id) ON DELETE CASCADE,
    UNIQUE (DoctorId, DayOfWeek)
);
```

### Advantages

✅ Different schedules per day (e.g., Monday 9-5, Tuesday 10-6)
✅ Proper time types (TIME) for database-level validation
✅ Easy to query specific days
✅ Can disable individual days without affecting others
✅ Better for complex schedules

### Disadvantages

❌ More records (up to 7 per doctor)
❌ Slightly more complex queries

---

## Option 2: **Normalized Schedule with Exception Table**

### Structure

Keep weekly schedule, add exception table for one-off changes:

```sql
-- Main availability (weekly pattern)
CREATE TABLE Availabilities (
    Id INT PRIMARY KEY IDENTITY(1,1),
    DoctorId INT NOT NULL,
    StartTime TIME NOT NULL,
    EndTime TIME NOT NULL,
    SlotDuration INT NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1
);

-- Day-specific overrides
CREATE TABLE AvailabilityDays (
    Id INT PRIMARY KEY IDENTITY(1,1),
    AvailabilityId INT NOT NULL,
    DayOfWeek INT NOT NULL, -- 0-6
    StartTime TIME NULL, -- NULL = use default
    EndTime TIME NULL, -- NULL = use default
    IsActive BIT NOT NULL DEFAULT 1,
    FOREIGN KEY (AvailabilityId) REFERENCES Availabilities(Id) ON DELETE CASCADE,
    UNIQUE (AvailabilityId, DayOfWeek)
);

-- One-time exceptions (holidays, special dates)
CREATE TABLE AvailabilityExceptions (
    Id INT PRIMARY KEY IDENTITY(1,1),
    DoctorId INT NOT NULL,
    ExceptionDate DATE NOT NULL,
    IsAvailable BIT NOT NULL, -- false = unavailable, true = available with custom hours
    StartTime TIME NULL,
    EndTime TIME NULL,
    Reason NVARCHAR(500),
    FOREIGN KEY (DoctorId) REFERENCES Doctors(Id) ON DELETE CASCADE,
    UNIQUE (DoctorId, ExceptionDate)
);
```

### Advantages

✅ Flexible: weekly pattern + day overrides + date exceptions
✅ Handles holidays and special dates elegantly
✅ Can mark specific dates as available/unavailable
✅ Leaves can be stored as exceptions (IsAvailable = false)

### Disadvantages

❌ More complex queries (need to check exceptions)
❌ More tables to manage

---

## Option 3: **Calendar-Based Approach** (Most Flexible)

### Structure

Store availability as calendar events:

```sql
CREATE TABLE AvailabilitySlots (
    Id INT PRIMARY KEY IDENTITY(1,1),
    DoctorId INT NOT NULL,
    StartDateTime DATETIME2 NOT NULL,
    EndDateTime DATETIME2 NOT NULL,
    SlotDuration INT NOT NULL,
    IsRecurring BIT NOT NULL DEFAULT 0,
    RecurrencePattern NVARCHAR(100), -- e.g., "WEEKLY:MONDAY", "DAILY", "MONTHLY:FIRST_MONDAY"
    RecurrenceEndDate DATE NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    FOREIGN KEY (DoctorId) REFERENCES Doctors(Id) ON DELETE CASCADE
);

CREATE TABLE AvailabilityExceptions (
    Id INT PRIMARY KEY IDENTITY(1,1),
    DoctorId INT NOT NULL,
    ExceptionDate DATE NOT NULL,
    IsAvailable BIT NOT NULL,
    StartTime TIME NULL,
    EndTime TIME NULL,
    Reason NVARCHAR(500),
    FOREIGN KEY (DoctorId) REFERENCES Doctors(Id) ON DELETE CASCADE
);
```

### Advantages

✅ Most flexible - handles any schedule pattern
✅ Supports recurring patterns (weekly, monthly, etc.)
✅ Can handle complex business rules
✅ Leaves are just exceptions

### Disadvantages

❌ Most complex to implement
❌ Requires recurrence logic
❌ Can be overkill for simple schedules

---

## Option 4: **Hybrid: Weekly Schedule + Exception Table** (Balanced)

### Structure

Keep simple weekly schedule, add exception table for leaves and special dates:

```sql
-- Simplified weekly availability
CREATE TABLE Availabilities (
    Id INT PRIMARY KEY IDENTITY(1,1),
    DoctorId INT NOT NULL,
    DayOfWeek INT NOT NULL, -- 0-6
    StartTime TIME NOT NULL,
    EndTime TIME NOT NULL,
    SlotDuration INT NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    FOREIGN KEY (DoctorId) REFERENCES Doctors(Id) ON DELETE CASCADE,
    UNIQUE (DoctorId, DayOfWeek)
);

-- Exceptions for leaves, holidays, special dates
CREATE TABLE AvailabilityExceptions (
    Id INT PRIMARY KEY IDENTITY(1,1),
    DoctorId INT NOT NULL,
    ExceptionDate DATE NOT NULL,
    IsAvailable BIT NOT NULL DEFAULT 0, -- false = on leave/unavailable
    CustomStartTime TIME NULL, -- optional override hours
    CustomEndTime TIME NULL,
    Reason NVARCHAR(500),
    ExceptionType INT NOT NULL, -- 0=Leave, 1=Holiday, 2=SpecialHours, etc.
    FOREIGN KEY (DoctorId) REFERENCES Doctors(Id) ON DELETE CASCADE,
    UNIQUE (DoctorId, ExceptionDate)
);
```

### Advantages

✅ Simple weekly pattern (easy to understand)
✅ Exceptions handle leaves and special dates
✅ Can merge Leaves table into Exceptions
✅ Good balance of simplicity and flexibility

### Disadvantages

❌ Still need to check exceptions when querying
❌ Two tables to maintain

---

## Recommendation: **Option 4 (Hybrid Approach)**

### Why?

1. **Simplicity**: Weekly schedule is easy to understand and manage
2. **Flexibility**: Exceptions handle leaves, holidays, and special dates
3. **Unified**: Can merge Leaves into AvailabilityExceptions
4. **Queryable**: Proper types (TIME, INT) instead of JSON strings
5. **Scalable**: Can add more exception types later

### Migration Path

1. Create new `AvailabilityDays` table (one row per day)
2. Create `AvailabilityExceptions` table (replaces Leaves)
3. Migrate existing data from `WorkingDaysJson` to `AvailabilityDays`
4. Migrate existing Leaves to `AvailabilityExceptions`
5. Update service layer to query new structure
6. Drop old `Availabilities` table and `Leaves` table

---

## Implementation Example (Option 4)

### Entity Classes

```csharp
public class AvailabilityDay
{
    public int Id { get; set; }
    public int DoctorId { get; set; }
    public Doctor Doctor { get; set; } = null!;
    public DayOfWeek DayOfWeek { get; set; } // 0-6
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public int SlotDuration { get; set; } // minutes
    public bool IsActive { get; set; } = true;
}

public class AvailabilityException
{
    public int Id { get; set; }
    public int DoctorId { get; set; }
    public Doctor Doctor { get; set; } = null!;
    public DateOnly ExceptionDate { get; set; }
    public bool IsAvailable { get; set; } = false; // false = on leave
    public TimeOnly? CustomStartTime { get; set; } // optional override
    public TimeOnly? CustomEndTime { get; set; }
    public string Reason { get; set; } = string.Empty;
    public ExceptionType Type { get; set; } // Leave, Holiday, SpecialHours
}

public enum ExceptionType
{
    Leave = 0,
    Holiday = 1,
    SpecialHours = 2
}
```

### Query Logic

```csharp
public async Task<IEnumerable<SlotDto>> GetAvailableSlotsAsync(int doctorId, DateOnly date)
{
    // 1. Check if there's an exception for this date
    var exception = await _exceptionRepository.GetByDoctorAndDateAsync(doctorId, date);
    if (exception != null && !exception.IsAvailable)
        return Enumerable.Empty<SlotDto>(); // On leave

    // 2. Get weekly schedule for this day
    var dayOfWeek = date.DayOfWeek;
    var availability = await _availabilityRepository.GetByDoctorAndDayAsync(doctorId, dayOfWeek);
    if (availability == null || !availability.IsActive)
        return Enumerable.Empty<SlotDto>();

    // 3. Use exception hours if provided, otherwise use weekly schedule
    var startTime = exception?.CustomStartTime ?? availability.StartTime;
    var endTime = exception?.CustomEndTime ?? availability.EndTime;

    // 4. Generate slots (existing logic)
    // ...
}
```

---

## Comparison Summary

| Feature                 | Current | Option 1 | Option 2 | Option 3  | Option 4 |
| ----------------------- | ------- | -------- | -------- | --------- | -------- |
| Different hours per day | ❌      | ✅       | ✅       | ✅        | ✅       |
| Proper time types       | ❌      | ✅       | ✅       | ✅        | ✅       |
| Queryable days          | ❌      | ✅       | ✅       | ✅        | ✅       |
| Handle leaves           | ✅      | ✅       | ✅       | ✅        | ✅       |
| Handle holidays         | ❌      | ❌       | ✅       | ✅        | ✅       |
| Handle special dates    | ❌      | ❌       | ✅       | ✅        | ✅       |
| Complexity              | Low     | Medium   | High     | Very High | Medium   |
| Migration effort        | -       | Low      | Medium   | High      | Medium   |

---

## Next Steps

1. **Choose an option** based on your requirements
2. **Create migration** to implement new structure
3. **Update entities** and repositories
4. **Update service layer** with new query logic
5. **Update API endpoints** if needed
6. **Migrate existing data**
7. **Test thoroughly**

Would you like me to implement one of these options?




