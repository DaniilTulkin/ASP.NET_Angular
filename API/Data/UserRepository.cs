using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.DTOs;
using API.Entities;
using API.Interfaces;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;

namespace API.Data
{
    public class UserRepository : IUserRepository
    {
        private readonly DataContext context;
        private readonly IMapper mapper;

        public UserRepository(DataContext context, IMapper mapper)
        {
            this.mapper = mapper;
            this.context = context;
        }

        public async Task<MemberDTO> GetMemberByNameAsync(string userName) => 
            await context.Users
                .Where(x => x.UserName == userName)
                .ProjectTo<MemberDTO>(mapper.ConfigurationProvider)
                .SingleOrDefaultAsync();

        public async Task<IEnumerable<MemberDTO>> GetMembersAsync() => 
            await context.Users
                .ProjectTo<MemberDTO>(mapper.ConfigurationProvider)
                .ToListAsync();

        public async Task<AppUser> GetUserByIdAsync(int id) =>
            await context.Users.FindAsync(id);

        public async Task<AppUser> GetUserByNameAsync(string userName) => 
            await context.Users
                .Include(p => p.Photos)
                .SingleOrDefaultAsync(x => x.UserName == userName);

        public async Task<IEnumerable<AppUser>> GetUsersAsync() => 
            await context.Users.Include(p => p.Photos).ToListAsync();

        public async Task<bool> SaveAllAsync() => 
            await context.SaveChangesAsync() > 0;

        public void Update(AppUser user) => 
            context.Entry(user).State = EntityState.Modified;
    }
}