//# STOPIFNOT FULL
call scripts/init/animinit.tcl



def_class Dummy_Kris_wand_q none dummy 1 {} {
	call scripts/misc/animclassinit.tcl
	class_disablescripting
	class_defaultanim kris_wand_q.standard
	class_particle 21 {-0.05 -0.75 0.4} {0 -0.005 0} 60 1 0 0 0.5
	class_particle 7 {-0.05 -0.9 0.4} {0 0 0.025} 60 1 0 0 0.5
	class_viewinfog 1
}


def_class Dummy_Kris_wand_p none dummy 1 {} {
	call scripts/misc/animclassinit.tcl
	class_disablescripting
	class_defaultanim kris_wand_p.standard
	class_particle 21 {-0.05 -0.75 0.4} {0 -0.005 0} 60 1 0 0 0.5
	class_particle 7 {-0.05 -1 0.4} {0 -0.05 0} 60 1 0 0 0.35
	class_viewinfog 1
}



def_class Dummy_Kris_kerzenhalter_wand_a none dummy 0 {} {
	call scripts/misc/animclassinit.tcl
	class_disablescripting
	class_defaultanim kris_kerzenhalter_wand_a.standard
	class_particle 16 {0 -3 0} {0.6 2 0} 32 2 0 0 -1
	class_viewinfog 1
}

def_class Dummy_Kris_kerzenhalter_wand_b none dummy 0 {} {
	call scripts/misc/animclassinit.tcl
	class_disablescripting
	class_light {0 -0.5 1} 2 {0.9 0.55 0.25}
	class_defaultanim kris_kerzenhalter_wand_b.standard
	class_particle 16 {-0.06 -0.3 1.2} {0.2 0 0} 32 2 0 0 -1
	class_viewinfog 1
}

def_class Dummy_Kris_kerzenhalter_boden_a none dummy 0 {} {
	call scripts/misc/animclassinit.tcl
	class_disablescripting
	class_defaultanim kris_kerzenhalter_boden_a.standard
	class_particle 16 {0 -5 0} {0.9 0.6 0} 32 2 0 0 -1
	class_viewinfog 1
}

def_class Dummy_Kris_kerzenhalter_boden_b none dummy 0 {} {
	call scripts/misc/animclassinit.tcl
	class_disablescripting
	class_light {0 -0.5 -0.5} 4 {0.9 0.55 0.25}
	class_defaultanim kris_kerzenhalter_boden_b.standard
	class_particle 0 {0 -1.75 -0.16} {0 0 0} 32 1 0 0 -1
	class_viewinfog 1
}

def_class Dummy_Kris_wandlicht_a none dummy 0 {} {
	call scripts/misc/animclassinit.tcl
	class_disablescripting
	class_light {0 -0.5 1} 2 {0.9 0.55 0.25}
	class_defaultanim kris_wandlicht_a.standard
	class_particle 27 {0 -0.16 1.3} {0 0 0} 32 1 0 0 -1
	class_viewinfog 1
}

def_class Dummy_Kris_wandlicht_b none dummy 0 {} {
	call scripts/misc/animclassinit.tcl
	class_disablescripting
	class_light {0 -0.5 1} 2 {0.9 0.55 0.25}
	class_defaultanim kris_wandlicht_b.standard
	class_particle 2 {0 -0.16 0.8} {0 0 0} 32 1 0 0 -1
	class_viewinfog 1
}

def_class Dummy_Kris_licht_b none dummy 0 {} {
	call scripts/misc/animclassinit.tcl
	class_disablescripting
	class_light {0 -0.5 -0.5} 4 {0.9 0.55 0.25}
	class_defaultanim kris_licht_b.standard
	class_particle 0 {0 -1.5 0} {0 0 0} 32 1 0 0 -1
	class_viewinfog 1
}


def_class Dummy_Kris_licht_a none dummy 0 {} {
	call scripts/misc/animclassinit.tcl
	class_disablescripting
	class_light {0 -0.5 -0.5} 2.5 {0.9 0.55 0.25}
	class_defaultanim kris_licht_a.standard
	class_particle 0 {0 -1.5 0} {0 0 0} 32 1 0 0 -1
	class_viewinfog 1
}



def_class Dummy_Kris_licht_c none dummy 0 {} {
	call scripts/misc/animclassinit.tcl
	class_disablescripting
	class_light {0 -0.5 -0.5} 4 {0.1 0.1 0.4}
	class_defaultanim kris_licht_b.standard
	class_particle 30 {0 -1.5 0} {0 0 0} 32 1 0 0 -1
	class_viewinfog 1
}


SetFrontDummyClassesNoPhys {
{ Abschluss_kris_a abschluss_kris_a }
{ Abschluss_kris_b abschluss_kris_b }
{ Ecke_kris_a ecke_kris_a }

{ Kris_einzel_a kris_einzel_a }
{ Kris_einzel_b kris_einzel_b }
{ Kris_einzel_c kris_einzel_c }
{ Kris_einzel_d kris_einzel_d }
{ Kris_einzel_e kris_einzel_e }
{ Kris_einzel_f kris_einzel_f }
{ Kris_einzel_g kris_einzel_g }
{ Kris_einzel_h kris_einzel_h }
{ Kris_einzel_i kris_einzel_i }
{ Kris_einzel_j kris_einzel_j }
{ Kris_einzel_k kris_einzel_k }
{ Kris_einzel_l kris_einzel_l }
{ Kris_einzel_m kris_einzel_m }
{ Kris_einzel_n kris_einzel_n }
{ Kris_einzel_o kris_einzel_o }
{ Kris_einzel_p kris_einzel_p }
{ Kris_einzel_aa kris_einzel_aa }
{ Kris_einzel_bb kris_einzel_bb }
{ Kris_einzel_cc kris_einzel_cc }
{ Kris_einzel_dd kris_einzel_dd }
{ Kris_einzel_ee kris_einzel_ee }
{ Kris_einzel_ff kris_einzel_ff }
{ Kris_einzel_gg kris_einzel_gg }
{ Kris_einzel_hh kris_einzel_hh }
{ Kris_einzel_ii kris_einzel_ii }
{ Kris_einzel_jj kris_einzel_jj }
{ Kris_einzel_kk kris_einzel_kk }
{ Kris_einzel_ll kris_einzel_ll }
{ Kris_einzel_mm kris_einzel_mm }
// { Kris_einzel_nn kris_einzel_nn }
// { Kris_einzel_oo kris_einzel_oo }
// { Kris_einzel_pp kris_einzel_pp }
// { Kris_kerzenhalter_wand_c kris_kerzenhalter_wand_c }
{ Kris_kerzenhalter_wand_d kris_kerzenhalter_wand_d }
{ Kris_kristall_a kris_kristall_a }
{ Kris_kristall_b kris_kristall_b }
// { Kris_kristall_c kris_kristall_c }
{ Kris_kristall_d kris_kristall_d }
{ Kris_kristall_e kris_kristall_e }
{ Kris_kristall_f kris_kristall_f }
{ Kris_kristall_g kris_kristall_g }
{ Kris_kristall_h kris_kristall_h }
// { Kris_kristall_i kris_kristall_i }
{ Kris_kristall_j kris_kristall_j }
{ Kris_kristall_k kris_kristall_k }
{ Kris_kristall_l kris_kristall_l }
{ Kris_kristall_m kris_kristall_m }
{ Kris_kristall_n kris_kristall_n }
{ Kris_kristall_o kris_kristall_o }
// { Kris_kristall_p kris_kristall_p }
// { Kris_kristall_q kris_kristall_q }
// { Kris_riesenwand kris_riesenwand }
// { Kris_seule_a kris_seule_a }
// { Kris_seule_b kris_seule_b }
// { Kris_seule_c kris_seule_c }
// { Kris_seule_d kris_seule_d }
{ Kris_seule_e kris_seule_e }
// { Kris_seule_f kris_seule_f }
// { Kris_seule_g kris_seule_g }
// { Kris_seule_h kris_seule_h }
{ Kris_tor_burg kris_tor_burg }
{ Kris_tor_burg_tl kris_tor_burg_tl }
{ Kris_tor_burg_tr kris_tor_burg_tr }
// { Kris_wand_a kris_wand_a }
// { Kris_wand_aa kris_wand_aa }
{ Kris_wand_ab kris_wand_ab }
// { Kris_wand_ac kris_wand_ac }
{ Kris_wand_ad kris_wand_ad }
// { Kris_wand_b kris_wand_b }
// { Kris_wand_c kris_wand_c }
// { Kris_wand_d kris_wand_d }
{ Kris_wand_e kris_wand_e }
// { Kris_wand_f kris_wand_f }
{ Kris_wand_g kris_wand_g }
// { Kris_wand_h kris_wand_h }
// { Kris_wand_i kris_wand_i }
// { Kris_wand_j kris_wand_j }
{ Kris_wand_k kris_wand_k }
{ Kris_wand_l kris_wand_l }
{ Kris_wand_m kris_wand_m }
{ Kris_wand_n kris_wand_n }
{ Kris_wand_o kris_wand_o }


{ Kris_wand_r kris_wand_r }
{ Kris_wand_s kris_wand_s }
{ Kris_wand_t kris_wand_t }
{ Kris_wand_u kris_wand_u }

// { Kris_maschine kris_maschine }


{ Kris_bodengitter_a kris_bodengitter_a }
{ Kris_bodengitter_b kris_bodengitter_b }
// { Kris_bodengitter_c kris_bodengitter_c }
{ Kris_bodengitter_d kris_bodengitter_d }
{ Kris_bodengitter_e kris_bodengitter_e }
{ Kris_bodengitter_f kris_bodengitter_f }
{ Kris_bodengitter_g kris_bodengitter_g }


{ Kris_balken_a kris_balken_a }
// { Kris_balken_b kris_balken_b }
// { Kris_balken_c kris_balken_c }
// { Kris_balken_d kris_balken_d }
// { Kris_balken_e kris_balken_e }
{ kris_brunnen kris_brunnen }
// { Kris_bogen_a kris_bogen_a }
// { Kris_bogen_b kris_bogen_b }
// { Kris_bogen_c kris_bogen_c }
// { Kris_bogen_d kris_bogen_d }
// { Kris_bogen_e kris_bogen_e }
// { Kris_bogen_f kris_bogen_f }
{ Kris_bogen_g kris_bogen_g }
// { Kris_bogen_h kris_bogen_h }
{ Kris_bogen_i kris_bogen_i }
// { Kris_bogen_j kris_bogen_j }
// { Kris_bogen_k kris_bogen_k }
{ Kris_zinnen_a kris_zinnen_a }
{ Kris_zinnen_b kris_zinnen_b }
{ Kris_zinnen_c kris_zinnen_c }
{ Kris_zinnen_d kris_zinnen_d }
// { Kris_zinnen_e kris_zinnen_e }
// { Kris_zinnen_f kris_zinnen_f }
{ Kris_zinnen_g kris_zinnen_g }
{ Kris_zinnen_h kris_zinnen_h }
{ Kris_zinnen_i kris_zinnen_i }
{ Kris_zinnen_j kris_zinnen_j }
{ Kris_zinnen_k kris_zinnen_k }
{ Kris_zinnen_l kris_zinnen_l }
// { Kris_wandlicht_c kris_wandlicht_c }


{ Kris_turmring_a kris_turmring_a }
{ Kris_turmring_aa kris_turmring_aa }
// { Kris_turmring_ab kris_turmring_ab }
{ Kris_turmring_ac kris_turmring_ac }
// { Kris_turmring_b kris_turmring_b }
// { Kris_turmring_ba kris_turmring_ba }
// { Kris_turmring_bb kris_turmring_bb }
// { Kris_turmring_bc kris_turmring_bc }
{ Kris_turmabschluss_a kris_turmabschluss_a }
{ Kris_turmabschluss_b kris_turmabschluss_b }

{ Kris_turm_a kris_turm_a }
{ Kris_turm_b kris_turm_b }
{ Ecke_kris_b ecke_kris_b }
{ Ecke_kris_c ecke_kris_c }
{ Ecke_kris_d ecke_kris_d }

// { Kris_lorelei kris_lorelei }
// { Kris_tomblume_a kris_tomblume_a }
// { Kris_tomblume_b kris_tomblume_b }
{ Kris_grass_a kris_grass_a }
{ Kris_grass_b kris_grass_b }
// { Kris_grass_c kris_grass_c }
// { Kris_pilz_a kris_pilz_a }
// { Kris_pilz_b kris_pilz_b }
// { Kris_pilz_c kris_pilz_c }
// { Kris_pilz_d kris_pilz_d }
// { Kris_pilz_e kris_pilz_e }
// { Kris_pilz_f kris_pilz_f }
{ Kris_thron kris_thron }
// { Kris_palast kris_palast }
{ Kris_uv_a kris_uv_a }
{ Kris_uv_b kris_uv_b }
{ Kris_uv_c kris_uv_c }
{ Kris_uv_d kris_uv_d }
{ Kris_uv_e kris_uv_e }
{ Kris_uv_f kris_uv_f }
{ Kris_uv_g kris_uv_g }
{ Kris_uv_h kris_uv_h }
{ Kris_uv_i kris_uv_i }
{ Kris_uv_j kris_uv_j }
{ Kris_uv_k kris_uv_k }
{ Kris_uv_l kris_uv_l }
{ Kris_uv_m kris_uv_m }
{ Kris_uv_n kris_uv_n }
{ Kris_uv_o kris_uv_o }
{ Kris_uv_p kris_uv_p }
{ Kris_uv_q kris_uv_q }
{ Kris_uv_r kris_uv_r }
{ Kris_uv_s kris_uv_s }
// { Env_test env_test }
{ Kris_ov_a kris_ov_a }
{ Kris_ov_b kris_ov_b }
{ Kris_ov_c kris_ov_c }
{ Kris_ov_d kris_ov_d }
{ Kris_ov_e kris_ov_e }
{ Kris_ov_f kris_ov_f }
{ Kris_ov_g kris_ov_g }
{ Kris_ov_h kris_ov_h }
{ Kris_ov_i kris_ov_i }
{ Kris_ov_j kris_ov_j }
{ Kris_ov_k kris_ov_k }
{ Kris_ov_l kris_ov_l }
{ Kris_wand_brainy_a kris_wand_brainy_a }
{ Kris_wand_brainy_b kris_wand_brainy_b }
{ Kris_wand_brainy_c kris_wand_brainy_c }
{ Kris_wand_brainy_d kris_wand_brainy_d }
{ Kris_wand_brainy_e kris_wand_brainy_e }
{ Kris_wand_brainy_f kris_wand_brainy_f }
{ Kris_wand_brainy_g kris_wand_brainy_g }
{ Kris_wand_brainy_h kris_wand_brainy_h }
{ Kris_wand_brainy_i kris_wand_brainy_i }
{ Kris_wand_brainy_j kris_wand_brainy_j }
// { Kris_wand_brainy_k kris_wand_brainy_k }
{ Kris_wand_brainy_l kris_wand_brainy_l }
{ Kris_wand_brainy_m kris_wand_brainy_m }
{ Kris_wand_brainy_n kris_wand_brainy_n }

{ Kris_wandgitter_a kris_wandgitter_a }
// { Kris_wandgitter_b kris_wandgitter_b }
// { Kris_wandgitter_c kris_wandgitter_c }
// { Kris_wandgitter_d kris_wandgitter_d }
// { Kris_wandgitter_e kris_wandgitter_e }
// { Kris_wandgitter_f kris_wandgitter_f }
// { Kris_wandgitter_g kris_wandgitter_g }
{ Kris_wandgitter_h kris_wandgitter_h }
// { Kris_wandgitter_i kris_wandgitter_i }
// { Kris_wandgitter_j kris_wandgitter_j }
{ Kris_wandgittek_k kris_wandgitter_k }
// { Kris_wandgitter_l kris_wandgitter_l }
// { Kris_wandgitter_m kris_wandgitter_m }
{ Kris_wandgitter_n kris_wandgitter_n }
{ Kris_wandgitter_o kris_wandgitter_o }
{ Kris_wandgitter_p kris_wandgitter_p }
// { Kris_wandgitter_q kris_wandgitter_q }
// { Kris_wandgitter_r kris_wandgitter_r }
// { Kris_wandgitter_s kris_wandgitter_s }
// { Kris_wandgitter_t kris_wandgitter_t }
// { Kris_wandgitter_u kris_wandgitter_u }
{ Kris_wandgitter_v kris_wandgitter_v }

{ Kris_riesenturm kris_riesenturm }
{ Kris_gelaender_a kris_gelaender_a }
{ Kris_gelaender_b kris_gelaender_b }
{ Kris_gelaender_c kris_gelaender_c }
{ Kris_gelaender_d kris_gelaender_d }

{ Kris_glassplitter_a kris_glassplitter_a }
{ Kris_glassplitter_b kris_glassplitter_b }
{ Kris_glassplitter_c kris_glassplitter_c }
{ Kris_glassplitter_d kris_glassplitter_d }
{ Kris_glassplitter_e kris_glassplitter_e }
{ Kris_glassplitter_f kris_glassplitter_f }
{ Kris_glassplitter_g kris_glassplitter_g }
{ Kris_glassplitter_h kris_glassplitter_h }
{ Kris_glassplitter_i kris_glassplitter_i }
{ Kris_glassplitter_j kris_glassplitter_j }
{ Kris_glassplitter_k kris_glassplitter_k }
{ Kris_glassplitter_l kris_glassplitter_l }
{ Kris_glassplitter_m kris_glassplitter_m }
{ Kris_glassplitter_n kris_glassplitter_n }
{ Kris_glassplitter_o kris_glassplitter_o }
{ Kris_glassplitter_p kris_glassplitter_p }
{ Kris_glassplitter_q kris_glassplitter_q }
{ Kris_glassplitter_r kris_glassplitter_r }
{ Kris_glassplitter_s kris_glassplitter_s }
{ Kris_glassplitter_t kris_glassplitter_t }
{ Kris_glassplitter_u kris_glassplitter_u }
{ Kris_glassplitter_v kris_glassplitter_v }
{ Kris_glassplitter_w kris_glassplitter_w }

{ Kris_um_a kris_um_a }	
{ Kris_um_b kris_um_b }	
{ Kris_um_c kris_um_c }	
{ Kris_um_d kris_um_d }	
{ Kris_um_e kris_um_e }	
{ Kris_um_f kris_um_f }	
{ Kris_um_g kris_um_g }	
{ Kris_um_h kris_um_h }	
{ Kris_um_i kris_um_i }	
{ Kris_um_j kris_um_j }	
{ Kris_um_k kris_um_k }	
{ Kris_um_l kris_um_l }	
{ Kris_um_m kris_um_m }	
{ Kris_um_n kris_um_n }	
{ Kris_um_o kris_um_o }	
{ Kris_um_p kris_um_p }	
{ Kris_um_q kris_um_q }	

{ Abschluss_kris_brainy_a abschluss_kris_brainy_a }	
{ Abschluss_kris_brainy_b abschluss_kris_brainy_b }	
{ Abschluss_kris_brainy_c abschluss_kris_brainy_c }	

{ Kris_decke_brainy_a kris_decke_brainy_a }
{ Kris_decke_brainy_b kris_decke_brainy_b }
{ Kris_decke_brainy_c kris_decke_brainy_c }
{ Kris_decke_brainy_d kris_decke_brainy_d }
// { Kris_decke_brainy_e kris_decke_brainy_e }
// { Kris_decke_brainy_f kris_decke_brainy_f }
// { Kris_decke_brainy_g kris_decke_brainy_g }
// { Kris_decke_brainy_h kris_decke_brainy_h }
// { Kris_decke_brainy_i kris_decke_brainy_i }

{ kris_treppe_a  kris_treppe_a }
{ kris_treppe_b  kris_treppe_b }

{ kris_lorelei_kris kris_lorelei_kris }

{ Brainposter_a  brainposter_a }
{ Brainposter_aa brainposter_aa }
{ Brainposter_b brainposter_b }
{ Brainposter_bb brainposter_bb }
{ Brainposter_c brainposter_c }
{ Brainposter_cc brainposter_cc }
{ Brainposter_d brainposter_d }
{ Brainposter_dd brainposter_dd }
{ Brainposter_e brainposter_e }
{ Brainposter_ee brainposter_ee }
{ Brainposter_f brainposter_f }
{ Brainposter_ff brainposter_ff }

{ Brain_kette_a brain_kette_a }
// { Brain_kette_aa brain_kette_aa }
{ Brain_kette_b brain_kette_b }
{ Brain_kette_bb brain_kette_bb }
// { Brain_seil_a brain_seil_a }
// { Brain_seil_aa brain_seil_aa }
// { Brain_seil_b brain_seil_b }
// { Brain_seil_bb brain_seil_bb }
{ Bierautomat bierautomat anim }
{ Kris_boden_q kris_boden_q }
{ Kris_boden_r kris_boden_r }
{ Kris_boden_s kris_boden_s }
{ Kris_boden_y kris_boden_y }
{ Kris_mittel_a kris_mittel_a }
{ Kris_mittel_b kris_mittel_b }

{ Skelett_a skelett_a }
{ Skelett_b skelett_b }
{ Skelett_c skelett_c }
{ Skelett_d skelett_d }
{ Skelett_e skelett_e }
{ Skelett_f skelett_f }
{ Skelett_g skelett_g }
{ Skelett_h skelett_h }
{ Skelett_i skelett_i }
// { Skelett_j skelett_j }
{ Skelett_k skelett_k }
{ Skelett_l skelett_l }
{ Skelett_m skelett_m }
// { Skelett_n skelett_n }

{ Kris_waffekapu_a kris_waffekapu_a }
{ Kris_waffekapu_b kris_waffekapu_b }
{ Kris_waffekapu_c kris_waffekapu_c }
{ Kris_waffekapu_d kris_waffekapu_d }
{ Kris_waffekapu_e kris_waffekapu_e }
{ Kris_waffekapu_f kris_waffekapu_f }

{ Kris_bodengitter_ende_a kris_bodengitter_ende_a }
// { Kris_bodengitter_ende_b kris_bodengitter_ende_b }
// { Kris_bodengitter_ende_c kris_bodengitter_ende_c }
// { Kris_bodengitter_ende_d kris_bodengitter_ende_d }



{ Kris_riesenturm_b kris_riesenturm_b }

}


SetDummyClassesNoZ {

{ Kris_boden_a kris_boden_a }
{ Kris_boden_b kris_boden_b }
{ Kris_boden_c kris_boden_c }
{ Kris_boden_d kris_boden_d }
{ Kris_boden_e kris_boden_e }
// { Kris_boden_f kris_boden_f }
// { Kris_boden_g kris_boden_g }
// { Kris_boden_h kris_boden_h }

{ Kris_boden_n kris_boden_n }
{ Kris_boden_o kris_boden_o }
{ Kris_boden_p kris_boden_p }

{ Kris_boden_t kris_boden_t }
{ Kris_boden_u kris_boden_u }
{ Kris_boden_v kris_boden_v }
{ Kris_boden_w kris_boden_w }
{ Kris_boden_x kris_boden_x }

// { Kris_boden_z kris_boden_z }
{ Kris_boden_aa kris_boden_aa }
{ Kris_boden_ab kris_boden_ab }
{ Kris_boden_ac kris_boden_ac }
{ Kris_boden_ad kris_boden_ad }
{ Kris_boden_ae kris_boden_ae }
{ Kris_boden_af kris_boden_af }

{ Kris_boden_brainy_a kris_boden_brainy_a }
{ Kris_boden_brainy_b kris_boden_brainy_b }
// { Kris_boden_brainy_c kris_boden_brainy_c }
{ Kris_boden_brainy_d kris_boden_brainy_d }
{ Kris_boden_brainy_e kris_boden_brainy_e }
{ Kris_boden_brainy_f kris_boden_brainy_f }
{ Kris_boden_brainy_g kris_boden_brainy_g }
{ Kris_boden_brainy_h kris_boden_brainy_h }
// { Kris_boden_brainy_i kris_boden_brainy_i }


}


SetDummyClassesAll {noidiobj} {
	
{ Kris_tor kris_tor }

}

