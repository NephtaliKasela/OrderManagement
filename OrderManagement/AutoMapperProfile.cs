using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using OrderManagement.DTOs;
using OrderManagement.Models;

namespace OrderManagement
{
    public class AutoMapperProfile: Profile
    {
        public AutoMapperProfile()
        {
            CreateMap<Order, GetOrderDTO>();
            CreateMap<AddOrderDTO, Order>();
            //CreateMap<Weapon, GetWeaponDTO>();
            //CreateMap<Skill, GetSkillDTO>();
        }
    }
}