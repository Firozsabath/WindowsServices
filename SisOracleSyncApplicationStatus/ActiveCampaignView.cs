using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SisOracleSyncApplicationStatus
{
    class ActiveCampaignView
    {
        public ACContactSync contact { get; set; }
    }

    public class ACCustomFields
    {
        public string field { get; set; }
        public string value { get; set; }
    }

    public class ACContactSync
    {
        public string email { get; set; }
        public List<ACCustomFields> fieldValues { get; set; }
    }

    public class EloquaInteg
    {

        public string Email { get; set; }
        public string Acceptance_Letter { get; set; }
        public string StudentID { get; set; }
        public string Accepted_the_Offer { get; set; }
        public string Seat_Payment { get; set; }
        public string Student_Registered { get; set; }
        public string Application_Status { get; set; }
        public string entrance_term { get; set; }
        public string id { get; set; }
        public string ProspectID_c { get; set; }
    }

    public class EloquaContactview
    {
        public EloquaInteg contact { get; set; }
    }
}
