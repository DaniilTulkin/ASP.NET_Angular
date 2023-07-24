import { ActivatedRouteSnapshot, ResolveFn, RouterStateSnapshot } from "@angular/router";
import { inject } from "@angular/core";

import { Member } from "../_models/member";
import { MembersService } from "../_services/members.service";

export const memberDetailedResolver: ResolveFn<Member> =
    (route: ActivatedRouteSnapshot, state: RouterStateSnapshot) => {
        return inject(MembersService).getMember(route.paramMap.get('userName'));
    }