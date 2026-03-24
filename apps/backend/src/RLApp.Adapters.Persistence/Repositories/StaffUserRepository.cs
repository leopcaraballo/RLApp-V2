using Microsoft.EntityFrameworkCore;
using RLApp.Adapters.Persistence.Data;
using RLApp.Adapters.Persistence.Data.Models;
using RLApp.Domain.Aggregates;
using RLApp.Domain.ValueObjects;
using RLApp.Ports.Inbound;

namespace RLApp.Adapters.Persistence.Repositories;

public class StaffUserRepository : IStaffUserRepository
{
    private readonly AppDbContext _context;

    public StaffUserRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<StaffUser?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var record = await _context.StaffUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

        return record == null ? null : MapToAggregate(record);
    }

    public async Task<StaffUser?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        var record = await _context.StaffUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Username == username, cancellationToken);

        return record == null ? null : MapToAggregate(record);
    }

    public async Task<StaffUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var record = await _context.StaffUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        return record == null ? null : MapToAggregate(record);
    }

    public async Task AddAsync(StaffUser staffUser, CancellationToken cancellationToken = default)
    {
        var record = new StaffUserRecord
        {
            Id = staffUser.Id,
            Username = staffUser.Username,
            Email = staffUser.Email.Value,
            PasswordHash = staffUser.PasswordHash,
            Role = staffUser.Role.Value,
            IsActive = staffUser.IsActive,
            CreatedAt = staffUser.CreatedAt,
            UpdatedAt = staffUser.UpdatedAt
        };

        _context.StaffUsers.Add(record);
    }

    public async Task UpdateAsync(StaffUser staffUser, CancellationToken cancellationToken = default)
    {
        var record = await _context.StaffUsers
            .FirstOrDefaultAsync(u => u.Id == staffUser.Id, cancellationToken);

        if (record == null)
            throw new KeyNotFoundException($"Staff user {staffUser.Id} not found");

        record.Username = staffUser.Username;
        record.Email = staffUser.Email.Value;
        record.PasswordHash = staffUser.PasswordHash;
        record.Role = staffUser.Role.Value;
        record.IsActive = staffUser.IsActive;
        record.UpdatedAt = staffUser.UpdatedAt;

        _context.StaffUsers.Update(record);
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var record = await _context.StaffUsers
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        
        if (record != null)
        {
            _context.StaffUsers.Remove(record);
        }
    }

    private static StaffUser MapToAggregate(StaffUserRecord record)
    {
        // Reconstruct aggregate via reflection to access private constructor
        var staffUser = (StaffUser)Activator.CreateInstance(
            typeof(StaffUser),
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
            null,
            new object[] { 
                record.Id, 
                record.Username, 
                Email.Create(record.Email), 
                record.PasswordHash, 
                StaffRole.Create(record.Role) 
            },
            null
        )!;

        // Set private properties if needed (IsActive, CreatedAt, UpdatedAt)
        var type = typeof(StaffUser);
        
        type.GetProperty("IsActive")?.SetValue(staffUser, record.IsActive);
        type.GetProperty("CreatedAt")?.SetValue(staffUser, record.CreatedAt);
        type.GetProperty("UpdatedAt")?.SetValue(staffUser, record.UpdatedAt);

        return staffUser;
    }
}
