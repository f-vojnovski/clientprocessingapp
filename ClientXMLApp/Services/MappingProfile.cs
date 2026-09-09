using ClientXMLApp.Models;
using ClientXMLApp.Services.DTOs;
using AutoMapper;

namespace ClientXMLApp.Services
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Client, ViewClientDto>()
                .ForMember(dest => dest.Addresses, opt => opt.MapFrom(src => src.Addresses));

            CreateMap<ViewClientDto, Client>();
            CreateMap<AddClientDto, Client>()
                .ForMember(dest => dest.ID, opt => opt.Ignore())
                .ForMember(dest => dest.Addresses, opt => opt.MapFrom(src => src.Addresses));

            CreateMap<Address, AddressDto>().ReverseMap();
        }
    }
}
