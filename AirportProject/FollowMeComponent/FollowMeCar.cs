using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FollowMeComponent
{
    public class FollowMeCar
    {
        string followMeId;
        public FollowMeCar(int id)
        {
            followMeId = "FollowMe-" + id;
        }
        public string FollowMeId { get => followMeId; }
        public int LocationVertex { get; set; }
        public string PlaneId { get; set; }
        public List<int> Path { get; set; }
    }
}
