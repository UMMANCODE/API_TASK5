using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TASK3_Business.Dtos.GroupDtos;
using TASK3_Business.Exceptions;
using TASK3_Business.Services.Interfaces;
using TASK3_Core.Entities;
using TASK3_DataAccess;
using TASK3_DataAccess.Repositories.Interfaces;

namespace TASK3_Business.Services.Implementations {
  public class GroupService : IGroupService {
    private readonly IGroupRepository _groupRepository;

    public GroupService(IGroupRepository groupRepository) {
      _groupRepository = groupRepository;
    }

    public async Task<int> Create(GroupCreateOneDto dto) {
      if (await _groupRepository.ExistsAsync(x => x.Name == dto.Name && !x.IsDeleted))
        throw new RestException(StatusCodes.Status400BadRequest, "Name", "Dublicate Name values");

      Group entity = new() {
        Name = dto.Name,
        Limit = dto.Limit
      };
      await _groupRepository.AddAsync(entity);
      await _groupRepository.SaveAsync();

      return entity.Id;
    }
    public async Task<List<GroupGetAllDto>> GetAll(int pageNumber = 1, int pageSize = 1) {
      if (pageNumber <= 0 || pageSize <= 0) {
        throw new RestException(StatusCodes.Status400BadRequest, "Invalid parameters for paging");
      }

      var groups = await _groupRepository.GetAllAsync(x => !x.IsDeleted, pageNumber, pageSize);
      return groups.Select(x => new GroupGetAllDto {
        Id = x.Id,
        Name = x.Name,
        Limit = x.Limit
      }).ToList();
    }
    public async Task<GroupGetOneDto> GetById(int id) {
      var group = await _groupRepository.GetAsync(x => x.Id == id && !x.IsDeleted);

      return group == null
        ? throw new RestException(StatusCodes.Status404NotFound, "Group not found")
        : new GroupGetOneDto {
          Id = group.Id,
          Name = group.Name,
          Limit = group.Limit
        };
    }
    public async Task Update(int id, GroupUpdateOneDto updateDto) {
      var group = await _groupRepository.GetAsync(x => x.Id == id && !x.IsDeleted, "Students")
        ?? throw new RestException(StatusCodes.Status404NotFound, "Group not found");

      if (group.Name != updateDto.Name && await _groupRepository.ExistsAsync(x => x.Name == updateDto.Name && !x.IsDeleted))
        throw new RestException(StatusCodes.Status400BadRequest, "Name", "Dublicate Name values");

      if (group.Students.Count > updateDto.Limit)
        throw new RestException(StatusCodes.Status400BadRequest, "Limit", "Limit overflow");

      group.Name = updateDto.Name;
      group.Limit = updateDto.Limit;
      group.UpdatedAt = DateTime.Now;

      await _groupRepository.SaveAsync();
    }

    public async Task Delete(int id) {
      var group = await _groupRepository.GetAsync(x => x.Id == id && !x.IsDeleted, "Students")
        ?? throw new RestException(StatusCodes.Status404NotFound, "Group not found");

      if (group.Students.Count > 0)
        throw new RestException(StatusCodes.Status400BadRequest, "Group", "Group has students");

      group.IsDeleted = true;
      group.UpdatedAt = DateTime.Now;

      await _groupRepository.SaveAsync();
    }
  }
}
