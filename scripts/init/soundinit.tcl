// soundinit.tcl

// notiz:  schlafen, hamster.laufen_loop, raupe...

proc snd_anim {animname frame soundname} {
	sound anim $animname $frame $soundname any any
}

proc snd_animlist {objname animlist} {
	set llen [llength $animlist]
//	log "snd_animlist $dbid $objname"
	for {set idx 0} {$idx<$llen} {incr idx} {
		set animname [lindex $animlist $idx]
		incr idx
		set framelist [lindex $animlist $idx]
		set flen [llength $framelist]
//		log " > anim $animname ($flen)"
		for {set fidx 0} {$fidx<$flen} {incr fidx} {
			set frame [lindex $framelist $fidx]
			incr fidx
			set sndname [lindex $framelist $fidx]
//			log " >> snd $fidx $frame $sndname"
			sound anim ${objname}.${animname} $frame $sndname
//	sound anim ${objname}.${animname} $frame $sndname any any 1.0 1.0 0.0 1.0
		}
	}
}

sound bind mp_chat_1		data/sound/sprache/m_ac_neg_a1.wav
sound bind mp_chat_2		data/sound/sprache/m_ac_neg_a2.wav
sound bind mp_chat_3		data/sound/sprache/m_ac_neg_a3.wav
sound bind mp_chat_4		data/sound/sprache/m_ac_neg_a4.wav
sound bind mp_chat_5		data/sound/sprache/m_ac_neg_a5.wav

catch {
sound bind mp_chat_111		data/sound/sprache/hos1.wav
sound bind mp_chat_222		data/sound/sprache/hos2.wav
sound bind mp_chat_333		data/sound/sprache/hos3.wav

sound bind mp_chat_123		data/sound/sprache/cow.wav
sound bind mp_chat_321		data/sound/sprache/sheep.wav
sound bind mp_chat_666		data/sound/sprache/terwin.wav
sound bind mp_chat_777		data/sound/sprache/ctwin.wav
sound bind mp_chat_101		data/sound/sprache/cs_ja.wav
sound bind mp_chat_102		data/sound/sprache/cs_nein.wav
sound bind mp_chat_69		data/sound/sprache/ct_fireinhole.wav
}

if { ! [minimalrun] } {

// Zwergensounds ausgelagert fuer kleinere Datei AxHy 310701
call scripts/init/z_soundinit.tcl
call scripts/init/t_soundinit.tcl

//--------------------Specials-bind----------------------------------

sound bind brainmasch_heil_zu_kaputt data/sound/Specials/heil_zu_kaputt.wav
sound bind brainmasch_kaputt_end data/sound/Specials/kaputt_e.wav
sound bind brainmasch_kaputt_loop data/sound/Specials/kaputt_l.wav
sound bind brainmasch_heil_loop data/sound/Specials/heil_loop.wav
sound bind Riesenlaufrad_kaputt data/sound/Specials/hamsterrad_kaputt.wav
sound bind riesenelfe_tod data/sound/Specials/absturz.wav
sound bind lore_fahren data/sound/Specials/fahren.wav
sound bind lore_crash data/sound/Specials/standanim.wav
sound bind foto_blitz data/sound/Specials/dummy_sound_2.wav
sound bind vampir_unheimlich data/sound/Specials/dummy_sound_3.wav
sound bind bruecke_crash data/sound/Specials/dummy_sound_4.wav
sound bind blood_splash data/sound/Landschaft/partikel_blut.wav

#die nase faellt ab
sound bind lorelei_knall data/sound/Specials/einsturz.wav

#das vorbereitende knacken
sound bind lorelei_knack data/sound/Specials/dummy_sound_12.wav

#der grosse chrash
sound bind lorelei_crash data/sound/Specials/treffer.wav

#der brocken
sound bind lorelei_brocken data/sound/Specials/dummy_sound_11.wav


sound bind zahnrad_a_loop data/sound/Specials/dummy_sound_9.wav


sound bind drachenei_auf data/sound/Specials/dummy_sound_5.wav
sound bind riesentor_auf data/sound/Specials/dummy_sound_6.wav
sound bind riesentor_zu data/sound/Specials/dummy_sound_7.wav
sound bind interface_hover data/sound/Specials/interface_hover.wav
sound bind interface_click data/sound/Specials/interface_klick.wav

sound bind tuer_schlag_a data/sound/kiste_ablegen.wav
sound bind tuer_schlag_b data/sound/specials/dummy_sound_13.wav
sound bind tuer_schlag_c data/sound/specials/dummy_sound_14.wav


//---------------------Zauber-bind-----------------------------------

sound bind magic_a data/sound/Landschaft/magic_a.wav
sound bind magic_b data/sound/Landschaft/magic_b.wav
sound bind magic_c data/sound/Landschaft/magic_c.wav
sound bind partikel_blut data/sound/Landschaft/partikel_blut.wav
sound bind partikel_explosion data/sound/Landschaft/partikel_explosion.wav
sound bind partikel_glitter data/sound/Landschaft/partikel_glitter.wav

//---------------------Erdbeben-bind-----------------------------------

sound bind equake7 data/sound/Wuker/WV_impresp.WAV
sound bind equake4 data/sound/Hamster/ham_karre_l.wav
sound bind equake10up data/sound/Fenris/InGame/fe_hauchend_m_k_1.wav

//---------------------Wasser-bind-----------------------------------

sound bind wasserfall data/sound/Specials/dummy_sound_1.wav

//---------------------BRUT-bind-----------------------------------

sound bind lbr_beissen data/sound/Brut/lbr_beissen.wav
sound bind lbr_beissen_b data/sound/Brut/lbr_beissen_b.wav
sound bind lbr_beissen_c data/sound/Brut/lbr_beissen_c.wav
sound bind lbr_bruellen_a data/sound/Brut/lbr_bruellen_a.wav
sound bind lbr_bruellen_b data/sound/Brut/lbr_bruellen_b.wav
sound bind lbr_fusstritt data/sound/Brut/lbr_fusstritt.wav
sound bind kbr_hinten_get_leicht data/sound/Brut/kbr_hinten_get_leicht.wav
sound bind kbr_hinten_get_m data/sound/Brut/kbr_hinten_get_m.wav
sound bind kbr_hinten_get_s1 data/sound/Brut/kbr_hinten_get_s1.wav
sound bind kbr_hinten_get_s2 data/sound/Brut/kbr_hinten_get_s2.wav
sound bind lbr_hinten_get_t1 data/sound/Brut/lbr_hinten_get_t1.wav
sound bind lbr_hinten_get_t2 data/sound/Brut/lbr_hinten_get_t2.wav
sound bind kbr_hornstoss data/sound/Brut/kbr_hornstoss.wav
sound bind kbr_laufen_loop data/sound/Brut/kbr_laufen_loop.wav
sound bind kbr_pinkeln_end data/sound/Brut/kbr_pinkeln_end.wav
sound bind kbr_pinkeln_loop data/sound/Brut/kbr_pinkeln_loop.wav
sound bind kbr_pinkeln_start data/sound/Brut/kbr_pinkeln_start.wav
sound bind lbr_plattmach_reanim data/sound/Brut/lbr_plattmach_reanim.wav
sound bind lbr_plattmach_tot data/sound/Brut/lbr_plattmach_tot.wav
sound bind lbr_rollen_loop data/sound/Brut/lbr_rollen_loop.wav
sound bind kbr_schleichen_end data/sound/Brut/kbr_schleichen_end.wav
sound bind kbr_schleichen_loop data/sound/Brut/kbr_schleichen_loop.wav
sound bind kbr_sprung data/sound/Brut/kbr_sprung.wav
sound bind lbr_sprungbeissen data/sound/Brut/lbr_sprungbeissen.wav
sound bind lbr_stampfen data/sound/Brut/lbr_stampfen.wav
sound bind lbr_trappeln1 data/sound/Brut/lbr_trappeln1.wav
sound bind lbr_trappeln2 data/sound/Brut/lbr_trappeln2.wav
sound bind lbr_umdrehen_l_180 data/sound/Brut/lbr_umdrehen_l_180.wav
sound bind kbr_umdrehen_l_90 data/sound/Brut/kbr_umdrehen_l_90.wav
sound bind lbr_umdrehen_r_180 data/sound/Brut/lbr_umdrehen_r_180.wav
sound bind kbr_umdrehen_r_90 data/sound/Brut/kbr_umdrehen_r_90.wav
sound bind lbr_versteinert_reanim data/sound/Brut/lbr_versteinert_reanim.wav
sound bind kbr_vorne_get_m data/sound/Brut/kbr_vorne_get_m.wav
sound bind kbr_vorne_get_s1 data/sound/Brut/kbr_vorne_get_s1.wav
sound bind kbr_vorne_get_s2 data/sound/Brut/kbr_vorne_get_s2.wav
sound bind lbr_vorne_get_t data/sound/Brut/lbr_vorne_get_t.wav


//---------------------DRACHE-bind---------------------------------

sound bind fluegelschlag data/sound/Drache/fluegelschlag.wav
sound bind liegen_d_zu_schulter_r_d_werschleicht data/sound/Drache/liegen_d_zu_schulter_r_d_werschleicht.WAV
sound bind pumpe_zu_sitzen data/sound/Drache/pumpe_zu_sitzen.wav
sound bind schritt data/sound/Drache/sitzen_gehen.wav
sound bind schulter_r_speien data/sound/Drache/schulter_r_speien.wav
sound bind sitzen_gehen data/sound/Drache/schritt.wav
sound bind sitzen_get_h_l data/sound/Drache/sitzen_get_h_l.wav
sound bind sitzen_get_h_m data/sound/Drache/sitzen_get_h_m.wav
sound bind sitzen_get_toedlich1 data/sound/Drache/sitzen_get_toedlich1.wav
sound bind sitzen_get_toedlich2 data/sound/Drache/sitzen_get_toedlich2.wav
sound bind sitzen_get_v_l data/sound/Drache/sitzen_get_v_l.wav
sound bind sitzen_get_v_m data/sound/Drache/sitzen_get_v_m.wav
sound bind sitzen_knuddeln_end data/sound/Drache/sitzen_knuddeln_end.wav
sound bind sitzen_knuddeln_start data/sound/Drache/sitzen_knuddeln_start.wav
sound bind sitzen_peitschen data/sound/Drache/sitzen_peitschen.wav
sound bind sitzen_umdrehen data/sound/Drache/sitzen_umdrehen.wav
sound bind sitzen_zu_speien data/sound/Drache/sitzen_zu_speien.wav
sound bind speien_loop_4 data/sound/Drache/speien_loop_4.wav
sound bind speien_loop_7 data/sound/Drache/speien_loop_7.wav
sound bind speien_zu_sitzen data/sound/Drache/speien_zu_sitzen.wav
sound bind speien_zu_sitzen_schritte data/sound/Drache/speien_zu_sitzen_schritte.wav
sound bind sterben_zu_tot data/sound/Drache/sterben_zu_tot.wav

//---------------------DRACHE-INGAME-bind------------------------------

sound bind dr_angestrengt_s_m_2 data/sound/Drache/InGame/dr_angestrengt_s_m_2.wav
sound bind dr_beissen_m_k_1 data/sound/Drache/InGame/dr_beissen_m_k_1.wav
sound bind dr_schmerzen_m_k_1 data/sound/Drache/InGame/dr_schmerzen_m_k_1.wav
sound bind dr_schmerzen_m_m_1 data/sound/Drache/InGame/dr_schmerzen_m_m_1.wav
sound bind dr_schmerzen_m_m_2 data/sound/Drache/InGame/dr_schmerzen_m_m_2.wav
sound bind dr_schmerzen_s_l_1 data/sound/Drache/InGame/dr_schmerzen_s_l_1.wav
sound bind dr_schmerzen_s_m_1 data/sound/Drache/InGame/dr_schmerzen_s_m_1.wav
sound bind dr_schmerzen_w_k_1 data/sound/Drache/InGame/dr_schmerzen_w_k_1.wav
sound bind dr_schnarchen_m_l_1 data/sound/Drache/InGame/dr_schnarchen_m_l_1.wav
sound bind dr_schnarchen_m_l_4 data/sound/Drache/InGame/dr_schnarchen_m_l_4.wav
sound bind dr_schnarchen_s_l_1 data/sound/Drache/InGame/dr_schnarchen_s_l_1.wav
sound bind dr_schnarchen_s_l_2 data/sound/Drache/InGame/dr_schnarchen_s_l_2.wav
sound bind dr_schnarchen_w_l_3 data/sound/Drache/InGame/dr_schnarchen_w_l_3.wav
sound bind dr_trotzig_m_k_1 data/sound/Drache/InGame/dr_trotzig_m_k_1.wav
sound bind dr_wuetend_s_m_1 data/sound/Drache/InGame/dr_wuetend_s_m_1.wav

sound bind drma_angestrengt_s_m_1 data/sound/Drache/InGame/drma_angestrengt_s_m_1.wav
sound bind drma_schmerzen_m_k_1 data/sound/Drache/InGame/drma_schmerzen_m_k_1.wav
sound bind drma_schmerzen_m_l_1 data/sound/Drache/InGame/drma_schmerzen_m_l_1.wav
sound bind drma_schmerzen_m_m_1 data/sound/Drache/InGame/drma_schmerzen_m_m_1.wav
sound bind drma_schmerzen_s_l_1 data/sound/Drache/InGame/drma_schmerzen_s_l_1.wav
sound bind drma_schmerzen_w_m_1 data/sound/Drache/InGame/drma_schmerzen_w_m_1.wav




//---------------------DRACHE01-bind----------------------------------

sound bind fauchen1 data/sound/Drache01/fauchen1.wav
sound bind ohren data/sound/Drache01/ohren.wav
sound bind schulter_r_peitschen data/sound/Drache01/schulter_r_peitschen.wav
sound bind schulter_r_speien01 data/sound/Drache01/schulter_r_speien.wav
sound bind schulter_r_zu_sitzen data/sound/Drache01/schulter_r_zu_sitzen.wav
sound bind sitzen_beissen data/sound/Drache01/sitzen_beissen.wav
sound bind sitzen_ja data/sound/Drache01/sitzen_ja.wav
sound bind sitzen_knuddeln data/sound/Drache01/sitzen_knuddeln.wav
sound bind sitzen_kratzen data/sound/Drache01/sitzen_kratzen.wav
sound bind sitzen_nagen data/sound/Drache01/sitzen_nagen.wav
sound bind sitzen_nein data/sound/Drache01/sitzen_nein.wav
sound bind sitzen_zu_stehen data/sound/Drache01/sitzen_zu_stehen.wav
sound bind speien_end data/sound/Drache01/speien_end.wav
sound bind speien_loop data/sound/Drache01/speien_loop.wav
sound bind speien_oben data/sound/Drache01/speien_oben.wav
sound bind speien_start data/sound/Drache01/speien_start.wav
sound bind stehen_anspringen data/sound/Drache01/stehen_anspringen.wav
sound bind stehen_gehen_end data/sound/Drache01/stehen_gehen_end.wav
sound bind stehen_gehen_loop1 data/sound/Drache01/stehen_gehen_loop1.wav
sound bind stehen_get_h_l data/sound/Drache01/stehen_get_h_l.wav
sound bind stehen_get_h_m data/sound/Drache01/stehen_get_h_m.wav
sound bind stehen_get_h_s data/sound/Drache01/stehen_get_h_s.wav
sound bind stehen_get_v_m data/sound/Drache01/stehen_get_v_m.wav
sound bind stehen_get_v_s1 data/sound/Drache01/stehen_get_v_s1.wav
sound bind stehen_get_v_s2 data/sound/Drache01/stehen_get_v_s2.wav
sound bind stehen_get_v_t1 data/sound/Drache01/stehen_get_v_t1.wav
sound bind stehen_get_v_t2 data/sound/Drache01/stehen_get_v_t2.wav
sound bind stehen_hopsen_end data/sound/Drache01/stehen_hopsen_end.wav
sound bind stehen_hopsen_loop data/sound/Drache01/stehen_hopsen_loop.wav
sound bind stehen_laufen_end data/sound/Drache01/stehen_laufen_end.wav
sound bind stehen_laufen_loop data/sound/Drache01/stehen_laufen_loop.wav
sound bind stehen_schweben_end data/sound/Drache01/stehen_schweben_end.wav
sound bind stehen_schweben_loop data/sound/Drache01/stehen_schweben_loop.wav
sound bind stehen_schweben_oben_loop data/sound/Drache01/stehen_schweben_oben_loop.wav
sound bind stehen_schweben_start data/sound/Drache01/stehen_schweben_start.wav
sound bind stehen_trappeln data/sound/Drache01/stehen_trappeln.wav
sound bind stehen_umdrehen_l_180 data/sound/Drache01/stehen_umdrehen_l_180.wav
sound bind stehen_wenden1 data/sound/Drache01/stehen_wenden1.wav
sound bind stehen_wenden2 data/sound/Drache01/stehen_wenden2.wav
sound bind stehen_zu_sitzen data/sound/Drache01/stehen_zu_sitzen.wav


//---------------------EINRICHTUNG-bind-------------------------------

sound bind Steintuer_auf data/sound/produktion/Steintuer_auf.wav
sound bind Steintuer_zu data/sound/produktion/Steintuer_zu.wav
sound bind Metalltuer_auf data/sound/produktion/Metalltuer_auf.wav
sound bind Metalltuer_zu data/sound/produktion/Metalltuer_zu.wav
sound bind Kristalltuer_auf data/sound/produktion/Kristalltuer_auf.wav
sound bind Kristalltuer_zu data/sound/produktion/Kristalltuer_zu.wav



//---------------------FENRIS-bind----------------------------------

sound bind fe_doppelswish1 data/sound/fenris/fe_doppelswish1.wav
sound bind fe_fallen data/sound/fenris/fe_fallen.wav
sound bind fe_fausttisch data/sound/fenris/fe_fausttisch.wav
sound bind fe_fenris_zu_fiffi data/sound/fenris/fe_fenris_zu_fiffi.wav
sound bind fe_feuer2 data/sound/fenris/fe_feuer2.wav
sound bind fe_fiffi_zu_fenris data/sound/fenris/fe_fiffi_zu_fenris.wav
sound bind fe_haare data/sound/fenris/fe_haare.wav
sound bind fe_haare_waschen data/sound/fenris/fe_haare_waschen.wav
sound bind fe_lava1 data/sound/fenris/fe_lava1.wav
sound bind fe_lava2 data/sound/fenris/fe_lava2.wav
sound bind fe_lolli_haare data/sound/fenris/fe_lolli_haare.wav
sound bind fe_pflanzen_start data/sound/fenris/fe_pflanzen_start.wav
sound bind fe_quitschewiggle data/sound/fenris/fe_quitschewiggle.wav
sound bind fe_schlag data/sound/fenris/fe_schlag.wav
sound bind fe_schritt1 data/sound/fenris/fe_schritt1.wav
sound bind fe_schritt2 data/sound/fenris/fe_schritt2.wav
sound bind fe_setzten data/sound/fenris/fe_setzten.wav
sound bind fe_sprung data/sound/fenris/fe_sprung.wav
sound bind fe_swish_m_k_1 data/sound/fenris/fe_swish_m_k_1.wav
sound bind fe_swish_m_k_2 data/sound/fenris/fe_swish_m_k_2.wav
sound bind fe_swish_s_k_1 data/sound/fenris/fe_swish_s_k_1.wav
sound bind fe_swish_w_k_1 data/sound/fenris/fe_swish_w_k_1.wav
sound bind fe_swish_w_k_2 data/sound/fenris/fe_swish_w_k_2.wav
sound bind fe_swish_w_k_3 data/sound/fenris/fe_swish_w_k_3.wav

//---------------------FENRIS-INGAME-bind------------------------

sound bind fe_erleichtert_s_m_1 data/sound/fenris/ingame/fe_erleichtert_s_m_1.wav
sound bind fe_erschoepft_m_k_1 data/sound/fenris/ingame/fe_erschoepft_m_k_1.wav
sound bind fe_erschoepft_s_m_2 data/sound/fenris/ingame/fe_erschoepft_s_m_2.wav
sound bind fe_erschoepft_s_m_4 data/sound/fenris/ingame/fe_erschoepft_s_m_4.wav
sound bind fe_grummeln_s_l_1 data/sound/fenris/ingame/fe_grummeln_s_l_1.wav
sound bind fe_grummeln_s_l_2 data/sound/fenris/ingame/fe_grummeln_s_l_2.wav
sound bind fe_jaulend_s_m_1 data/sound/fenris/ingame/fe_jaulend_s_m_1.wav
sound bind fe_lachend_l_m_2 data/sound/fenris/ingame/fe_lachend_l_m_2.wav
sound bind fe_lachend_s_m_1 data/sound/fenris/ingame/fe_lachend_s_m_1.wav
sound bind fe_leidend_m_l_1 data/sound/fenris/ingame/fe_leidend_m_l_1.wav
sound bind fe_leidend_m_l_2 data/sound/fenris/ingame/fe_leidend_m_l_2.wav
sound bind fe_schmerzen_s_k_1 data/sound/fenris/ingame/fe_schmerzen_s_k_1.wav
sound bind fe_schmerzen_s_l_1 data/sound/fenris/ingame/fe_schmerzen_s_l_1.wav
sound bind fe_schreiend_m_m_1 data/sound/fenris/ingame/fe_schreiend_m_m_1.wav
sound bind fe_schreiend_m_m_2 data/sound/fenris/ingame/fe_schreiend_m_m_2.wav
sound bind fe_schreiend_s_m_1 data/sound/fenris/ingame/fe_schreiend_s_m_1.wav



//---------------------FIFI-bind------------------------------------

sound bind fifi_bein_heben data/sound/fifi/fifi_bein_heb.wav
sound bind fifi_geh_step1 data/sound/fifi/fifi_geh_step1.wav
sound bind fifi_geh_step2 data/sound/fifi/fifi_geh_step2.wav
sound bind fifi_hopsen data/sound/fifi/fifi_hopsen.wav
sound bind fifi_laufen_loop data/sound/fifi/fifi_laufen_l.wav
sound bind fifi_odin_arm data/sound/fifi/fifi_odin_arm.wav
sound bind fifi_gleip_kurz_a data/sound/fifi/fifi_gleip_kurz_a.wav
sound bind fifi_gleip_kurz_b data/sound/fifi/fifi_gleip_kurz_b.wav
sound bind fifi_gleip_kurz_c data/sound/fifi/fifi_gleip_kurz_c.wav
sound bind fifi_gleip_kurz_d data/sound/fifi/fifi_gleip_kurz_d.wav
sound bind fifi_gleip_ruhig_a data/sound/fifi/fifi_gleip_ruhig_a.wav
sound bind fifi_geh_trab1 data/sound/fifi/fifi_trab_step1.wav
sound bind fifi_geh_trab2 data/sound/fifi/fifi_trab_step2.wav

//---------------------HAMSTER-bind-------------------------------

sound bind ham_drehen data/sound/hamster/ham_drehen.wav
sound bind ham_hoppeln_l data/sound/hamster/ham_hoppeln_l.wav
sound bind ham_laufen_l data/sound/hamster/ham_laufen_l.wav
sound bind ham_laufrad data/sound/hamster/ham_laufrad.wav
sound bind ham_maennchen data/sound/hamster/ham_maennchen.wav
sound bind ham_putzen data/sound/hamster/ham_putzen.wav
sound bind ham_schlaf_e data/sound/hamster/ham_schlaf_e.wav
sound bind ham_schlaf_s data/sound/hamster/ham_schlaf_s.wav
sound bind ham_sterben data/sound/hamster/ham_sterben.wav


//---------------------KRAKE-bind------------------------------------

sound bind kr_glitsch data/sound/krake/kr_glitsch.wav
sound bind kr_glitschatmo data/sound/krake/kr_glitschatmo.wav
sound bind kr_swish1 data/sound/krake/kr_swish1.wav
sound bind kr_swish2 data/sound/krake/kr_swish2.wav
sound bind kr_swish3 data/sound/krake/kr_swish3.wav
sound bind kr_swish4 data/sound/krake/kr_swish4.wav
sound bind kr_swish5 data/sound/krake/kr_swish5.wav
sound bind kr_swish6 data/sound/krake/kr_swish6.wav
sound bind kr_swish7 data/sound/krake/kr_swish7.wav
sound bind kr_tasten_gold data/sound/krake/kr_tasten_gold.wav
sound bind kr_tod_durch_kronleuchter1 data/sound/krake/kr_tod_durch_kronleuchter1.wav
sound bind kr_tod_durch_kronleuchter2 data/sound/krake/kr_tod_durch_kronleuchter2.wav
sound bind kr_tod_durch_kronleuchter3 data/sound/krake/kr_tod_durch_kronleuchter3.wav
sound bind kr_treffer data/sound/krake/kr_treffer.wav
sound bind kr_wish data/sound/krake/kr_wish.wav


//---------------------LANDSCHAFT-bind-------------------------------

sound bind kette_rausziehen data/sound/Landschaft/kette_rausziehen.wav
sound bind lava_fliessen_a data/sound/Landschaft/lava_fliessen_a.wav
sound bind lava_fliessen_b data/sound/Landschaft/lava_fliessen_b.wav
sound bind lava_fliessen_c data/sound/Landschaft/lava_fliessen_c.wav
sound bind tuer_oeffnen data/sound/Landschaft/tuer_oeffnen.wav
sound bind tuer_schliessen data/sound/Landschaft/tuer_schliessen.wav


//---------------------PRODUKTIONSSTAETTEN-bind-------------------------


sound bind bett_anim data/sound/produktion/bett_anim.wav
sound bind bordell_oeffnen data/sound/produktion/bordell_oeffnen.wav
sound bind bordell_schliessen data/sound/produktion/bordell_schliessen.wav
sound bind Brauerei data/sound/produktion/Brauerei.wav

sound bind Dampfhammer data/sound/produktion/Dampfhammer.wav
sound bind Dampfmaschine data/sound/produktion/Dampfmaschine.wav
sound bind Dreherei data/sound/produktion/Dreherei.wav

sound bind Feuerstelle_1 data/sound/produktion/Feuerstelle_1.wav
sound bind Feuerstelle_2 data/sound/produktion/Feuerstelle_2.wav
sound bind Feuerstelle_3 data/sound/produktion/Feuerstelle_3.wav
sound bind Feuerstelle_4 data/sound/produktion/Feuerstelle_4.wav
sound bind Feuerstelle_5 data/sound/produktion/Feuerstelle_5.wav
sound bind Feuerstelle_6 data/sound/produktion/Feuerstelle_6.wav
sound bind Feuerstelle_7 data/sound/produktion/Feuerstelle_7.wav
sound bind Feuerstelle_8 data/sound/produktion/Feuerstelle_8.wav

sound bind KH_rollraus data/sound/produktion/KH_rollraus.wav
sound bind KH_Tisch data/sound/produktion/KH_Tisch.wav
sound bind HO_rad_drehen data/sound/produktion/HO_rad_drehen.wav
sound bind hochofen data/sound/produktion/hochofen.wav

sound bind Kristallschmiede data/sound/produktion/Kristallschmiede.wav

sound bind Labor data/sound/produktion/Labor.wav
sound bind Labor_kran data/sound/produktion/Labor_kran.wav

sound bind Moebeltischlerei data/sound/produktion/Moebeltischlerei.WAV

sound bind platt_hoch data/sound/produktion/platt_hoch.WAV
sound bind platt_runter data/sound/produktion/platt_runter.WAV

sound bind Reaktor data/sound/produktion/Reaktor.wav
sound bind Reaktor_dusche data/sound/produktion/Reaktor_dusche.wav

sound bind Disco data/sound/specials/dummy_sound_8.wav

sound bind Saegewerk data/sound/produktion/Saegewerk.WAV
sound bind Schleiferei data/sound/produktion/Schleiferei.WAV
sound bind schmelze data/sound/produktion/schmelze.WAV
sound bind tempel_ausfahr data/sound/produktion/tempel_ausfahr.WAV
sound bind tempel_einfahr data/sound/produktion/tempel_einfahr.WAV
sound bind tempel_einschalt data/sound/produktion/tempel_einschalt.WAV
sound bind tempel_pendel data/sound/produktion/tempel_pendel.WAV
sound bind waffenfabrik data/sound/produktion/waffenfabrik.WAV


//--------------------WUKER-bind--------------------------

sound bind flammenwerfer_a data/sound/specials/flammenw_a_loop.wav
sound bind flammenwerfer_b data/sound/specials/flammenw_b_loop.wav
sound bind flammenwerfer_c data/sound/specials/flammenw_c_loop.wav
sound bind flammenwerfer_start data/sound/specials/flammenw_start.wav
sound bind flammenwerfer_stop data/sound/specials/flammenw_end.wav



//--------------------WUKER-bind--------------------------

sound bind w_beissen_a data/sound/wuker/w_beissen_a.WAV
sound bind w_beissen_mitte data/sound/wuker/w_beissen_mitte.WAV
sound bind w_boxen data/sound/wuker/w_boxen.WAV
sound bind w_drehen_links data/sound/wuker/w_drehen_links.WAV
sound bind w_drehen_rechts data/sound/wuker/w_drehen_rechts.WAV
sound bind w_fliehen_e data/sound/wuker/w_fliehen_e.WAV
sound bind w_fliehen_l data/sound/wuker/w_fliehen_l.WAV
sound bind w_fliehen_s data/sound/wuker/w_fliehen_s.WAV
sound bind w_fressen_a data/sound/wuker/w_fressen_a.WAV
sound bind w_gehen_e data/sound/wuker/w_gehen_e.WAV
sound bind w_gehen_l data/sound/wuker/w_gehen_l.WAV
sound bind w_gehen_s data/sound/wuker/w_gehen_s.WAV
sound bind w_h_g_l data/sound/wuker/w_h_g_l.WAV
sound bind w_h_g_m data/sound/wuker/w_h_g_m.WAV
sound bind w_h_g_s data/sound/wuker/w_h_g_s.WAV
sound bind w_h_g_t data/sound/wuker/w_h_g_t.WAV
sound bind w_hopsen_a data/sound/wuker/w_hopsen_a.WAV
sound bind w_kletter_hoch data/sound/wuker/w_kletter_hoch.WAV
sound bind w_kletter_links data/sound/wuker/w_kletter_links.WAV
sound bind w_kletter_runter data/sound/wuker/w_kletter_runter.WAV
sound bind w_kletterst_stand data/sound/wuker/w_kletterst_stand.WAV
sound bind w_kopf_drehen_e data/sound/wuker/w_kopf_drehen_e.WAV
sound bind w_kopf_drehen_s data/sound/wuker/w_kopf_drehen_s.WAV
sound bind w_kratzen_a data/sound/wuker/w_kratzen_a.WAV
sound bind w_kratzen_b data/sound/wuker/w_kratzen_b.WAV
sound bind w_kratzen_c_e data/sound/wuker/w_kratzen_c_e.WAV
sound bind w_kratzen_c_s data/sound/wuker/w_kratzen_c_s.WAV
sound bind w_laufen_l data/sound/wuker/w_laufen_l.WAV
sound bind w_m_g_l data/sound/wuker/w_m_g_l.WAV
sound bind w_m_g_m data/sound/wuker/w_m_g_m.WAV
sound bind w_m_g_s data/sound/wuker/w_m_g_s.WAV
sound bind w_m_g_t data/sound/wuker/w_m_g_t.WAV
sound bind w_nachspringen_a data/sound/wuker/w_nachspringen_a.WAV
sound bind w_nachspringen_a_end data/sound/wuker/w_nachspringen_a_end.WAV
sound bind w_o_g_m data/sound/wuker/w_o_g_m.WAV
sound bind w_o_g_s data/sound/wuker/w_o_g_s.WAV
sound bind w_o_g_t data/sound/wuker/w_o_g_t.WAV
sound bind w_schlagen data/sound/wuker/w_schlagen.WAV
sound bind w_schnueffel data/sound/wuker/w_schnueffel.WAV
sound bind w_schnueffel_entd data/sound/wuker/w_schnueffel_entd.WAV
sound bind w_schnueffel_links data/sound/wuker/w_schnueffel_links.WAV
sound bind w_schnueffel_rechts data/sound/wuker/w_schnueffel_rechts.WAV
sound bind w_schuetteln data/sound/wuker/w_schuetteln.WAV
sound bind w_versteinert data/sound/wuker/w_versteinert.WAV
sound bind WV_aggression_a data/sound/wuker/WV_aggression_a.WAV
sound bind WV_aggression_b data/sound/wuker/WV_aggression_b.WAV
sound bind WV_aggression_c data/sound/wuker/WV_aggression_c.WAV
sound bind WV_lowrage_b data/sound/wuker/WV_lowrage_b.WAV
sound bind WV_normal_a data/sound/wuker/WV_normal_a.WAV
sound bind WV_suffer_a data/sound/wuker/WV_suffer_a.WAV
sound bind WV_suffer_b data/sound/wuker/WV_suffer_b.WAV
sound bind WV_tired_a data/sound/wuker/WV_tired_a.WAV


//--------------------------Wasser--------------------------

sound anim urw_wasserfall_a.standard 6 wasserfall any any 0.9 1.0 0 0

//--------------------------BRUT--------------------------

sound anim brut.beissen_a 6 lbr_beissen any any 0.9 1.0 0 0
sound anim brut.beissen_b 6 lbr_beissen_b any any 0.9 1.0 0 0
sound anim brut.beissen_c 6 lbr_beissen_c any any 0.9 1.0 0 0
sound anim brut.bruellen_a 6 lbr_bruellen_a any any 0.9 1.0 0 0
sound anim brut.bruellen_b 9 lbr_bruellen_b any any 0.9 1.0 0 0
sound anim brut.fusstritt 3 lbr_fusstritt any any 0.9 1.0 0 0
sound anim brut.hinten_get_leicht 1 kbr_hinten_get_leicht any any 0.9 1.0 0 0
sound anim brut.hinten_get_mittel 2 kbr_hinten_get_m any any 0.9 1.0 0 0
sound anim brut.hinten_get_schwer 2 kbr_hinten_get_s1 any any 0.9 1.0 0 0
sound anim brut.hinten_get_schwer 15 kbr_hinten_get_s2 any any 0.9 1.0 0 0
sound anim brut.hinten_get_tot 2 lbr_hinten_get_t1 any any 0.9 1.0 0 0
sound anim brut.hinten_get_tot 7 lbr_hinten_get_t2 any any 0.9 1.0 0 0
sound anim brut.hornstoss 2 kbr_hornstoss any any 0.9 1.0 0 0
sound anim brut.laufen_loop 1 kbr_laufen_loop any any 0.9 1.0 0 0
sound anim brut.pinkeln_end 0 kbr_pinkeln_end any any 0.9 1.0 0 0
sound anim brut.pinkeln_loop 0 kbr_pinkeln_loop any any 0.9 1.0 0 0
sound anim brut.pinkeln_start 10 kbr_pinkeln_start any any 0.9 1.0 0 0
sound anim brut.plattmach_reanim 2 lbr_plattmach_reanim any any 0.9 1.0 0 0
sound anim brut.plattmach_tot 3 lbr_plattmach_tot any any 0.9 1.0 0 0
sound anim brut.rollen_loop 0 lbr_rollen_loop any any 0.9 1.0 0 0
sound anim brut.schleichen_end 3 kbr_schleichen_end any any 0.9 1.0 0 0
sound anim brut.schleichen_loop 4 kbr_schleichen_loop any any 0.9 1.0 0 0
sound anim brut.sprung 7 kbr_sprung any any 0.9 1.0 0 0
sound anim brut.sprung_beissen 7 lbr_sprungbeissen any any 0.9 1.0 0 0
sound anim brut.stampfen 4 lbr_stampfen any any 0.9 1.0 0 0
sound anim brut.trappeln 6 lbr_trappeln1 any any 0.9 1.0 0 0
sound anim brut.trappeln 12 lbr_trappeln2 any any 0.9 1.0 0 0
sound anim brut.trappeln_schnell 3 lbr_trappeln2 any any 0.9 1.0 0 0
sound anim brut.trappeln_schnell 6 lbr_trappeln2 any any 0.9 1.0 0 0
sound anim brut.umdrehen_l_180 2 lbr_umdrehen_l_180 any any 0.9 1.0 0 0
sound anim brut.umdrehen_l_90 5 kbr_umdrehen_l_90 any any 0.9 1.0 0 0
sound anim brut.umdrehen_r_180 3 lbr_umdrehen_r_180 any any 0.9 1.0 0 0
sound anim brut.umdrehen_r_90 5 kbr_umdrehen_r_90 any any 0.9 1.0 0 0
sound anim brut.versteinert_reanim 0 lbr_versteinert_reanim any any 0.9 1.0 0 0
sound anim brut.vorne_get_mittel 3 kbr_vorne_get_m any any 0.9 1.0 0 0
sound anim brut.vorne_get_schwer 3 kbr_vorne_get_s1 any any 0.9 1.0 0 0
sound anim brut.vorne_get_schwer 14 kbr_vorne_get_s2 any any 0.9 1.0 0 0
sound anim brut.vorne_get_tot 3 lbr_vorne_get_t any any 0.9 1.0 0 0


//--------------------------DRACHE---------------------------

sound anim drache.liegen_d_zu_schulter_r_d_werschleicht 1 liegen_d_zu_schulter_r_d_werschleicht any any 0.9 1.0 0 0
sound anim drache.pumpe_zu_sitzen_end 6 pumpe_zu_sitzen any any 0.9 1.0 0 0
sound anim drache.schulter_l_d_ha 8 fluegelschlag any any 0.9 1.0 0 0
sound anim drache.schulter_r_d_willman 16 sitzen_umdrehen any any 0.9 1.0 0 0
sound anim drache.schulter_r_speien 1 schulter_r_speien any any 0.9 1.0 0 0
sound anim drache.schulter_l_speien 1 schulter_r_speien any any 0.9 1.0 0 0
// animation nicht da:    sound anim drache.schulter_r_zu_sitzen 10 schritt any any 0.9 1.0 0 0
sound anim drache.sitzen_get_hinten_leicht 3 sitzen_get_h_l any any 0.9 1.0 0 0
sound anim drache.sitzen_get_hinten_mittel 2 sitzen_get_h_m any any 0.9 1.0 0 0
sound anim drache.sitzen_get_toetlich 6 sitzen_get_toedlich1 any any 0.9 1.0 0 0
sound anim drache.sitzen_get_toetlich 17 sitzen_get_toedlich2 any any 0.9 1.0 0 0
sound anim drache.sitzen_get_vorne_leicht 1 sitzen_get_v_l any any 0.9 1.0 0 0
sound anim drache.sitzen_get_vorne_mittel 2 sitzen_get_v_m any any 0.9 1.0 0 0
sound anim drache.mama_sitzen_knuddeln_end 2 sitzen_knuddeln_end any any 0.9 1.0 0 0
sound anim drache.mama_sitzen_knuddeln_start 2 sitzen_knuddeln_start any any 0.9 1.0 0 0
sound anim drache.sitzen_peitschen 4 sitzen_peitschen any any 0.9 1.0 0 0
//sound anim drache.sitzen_speien_unten 2 sitzen_speien_unten any any 0.9 1.0 0 0
sound anim drache.sitzen_speien_unten 4 speien_loop_4 any any 0.9 1.0 0 0
sound anim drache.sitzen_speien_unten 7 speien_zu_sitzen any any 0.9 1.0 0 0

// hier fehlen die trollspeien-Sounds fuer den Beginn der Trollspeien-Sequenz
sound anim drache.sitzen_trollspeien 19 speien_zu_sitzen_schritte any any 0.9 1.0 0 0
sound anim drache.sitzen_umdrehen 5 schritt any any 0.9 1.0 0 0
sound anim drache.sitzen_umdrehen 20 sitzen_umdrehen any any 0.9 1.0 0 0
sound anim drache.sitzen_zu_pumpe_start 2 sitzen_knuddeln_start any any 0.9 1.0 0 0
sound anim drache.sitzen_zu_schulter_r 17 schritt any any 0.9 1.0 0 0
sound anim drache.liegen_d_zu_schulter_r_d_werschleicht 10 schritt any any 0.9 1.0 0 0
sound anim drache.sitzen_zu_speien_a 2 sitzen_zu_speien any any 0.9 1.0 0 0
sound anim drache.speien_a_loop 0 speien_loop_7 any any 0.9 1.0 0 0
sound anim drache.speien_a_loop 7 speien_loop_7 any any 0.9 1.0 0 0
sound anim drache.speien_a_zu_c 0 speien_loop_4 any any 0.9 1.0 0 0
sound anim drache.speien_a_zu_sitzen 0 speien_zu_sitzen any any 0.9 1.0 0 0
sound anim drache.speien_a_zu_sitzen 1 speien_zu_sitzen_schritte any any 0.9 1.0 0 0
sound anim drache.speien_b_loop 0 speien_loop_4 any any 0.9 1.0 0 0
sound anim drache.speien_b_loop 4 speien_loop_4 any any 0.9 1.0 0 0
sound anim drache.speien_b_loop 8 speien_loop_4 any any 0.9 1.0 0 0
sound anim drache.speien_b_zu_a 0 speien_loop_4 any any 0.9 1.0 0 0
sound anim drache.speien_b_zu_c 0 speien_loop_7 any any 0.9 1.0 0 0
sound anim drache.speien_b_zu_sitzen 0 speien_zu_sitzen any any 0.9 1.0 0 0
sound anim drache.speien_b_zu_sitzen 6 speien_zu_sitzen_schritte any any 0.9 1.0 0 0
sound anim drache.speien_c_loop 0 speien_loop_4 any any 0.9 1.0 0 0
sound anim drache.speien_c_loop 4 speien_loop_7 any any 0.9 1.0 0 0
sound anim drache.speien_c_zu_a 0 speien_loop_4 any any 0.9 1.0 0 0
sound anim drache.speien_c_zu_b 0 speien_loop_7 any any 0.9 1.0 0 0
sound anim drache.speien_c_zu_sitzen 0 speien_zu_sitzen any any 0.9 1.0 0 0
sound anim drache.speien_c_zu_sitzen 5 speien_zu_sitzen_schritte any any 0.9 1.0 0 0
sound anim drache.aufrichten_zu_tot 28 fluegelschlag any any 0.9 1.0 0 0
sound anim drache.aufrichten_zu_tot 40 sterben_zu_tot any any 0.9 1.0 0 0

//--------------------------DRACHE-INGAME-------------------------

sound anim drache.liegen_schlafen 3 dr_schnarchen_m_l_1 any any 0.9 1.0 0 0
sound anim drache.liegen_schlafen 3 dr_schnarchen_m_l_4 any any 0.9 1.0 0 0
sound anim drache.liegen_schlafen 3 dr_schnarchen_s_l_1 any any 0.9 1.0 0 0
sound anim drache.liegen_schlafen 3 dr_schnarchen_s_l_2 any any 0.9 1.0 0 0
sound anim drache.liegen_schlafen 3 dr_schnarchen_w_l_3 any any 0.9 1.0 0 0
sound anim drache.schulter_l_d_ha 15 dr_trotzig_m_k_1 any any 0.9 1.0 0 0
sound anim drache.sitzen_beissen 8 dr_beissen_m_k_1 any any 0.9 1.0 0 0
sound anim drache.sitzen_get_hinten_leicht 3 dr_schmerzen_w_k_1 any any 0.9 1.0 0 0
sound anim drache.sitzen_get_hinten_mittel 3 dr_schmerzen_m_k_1 any any 0.9 1.0 0 0
sound anim drache.sitzen_get_hinten_mittel 3 dr_schmerzen_m_m_1 any any 0.9 1.0 0 0
sound anim drache.sitzen_get_hinten_mittel 3 dr_schmerzen_m_m_2 any any 0.9 1.0 0 0
sound anim drache.sitzen_get_hinten_leicht 3 dr_schmerzen_w_k_1 any any 0.9 1.0 0 0
sound anim drache.sitzen_get_vorne_leicht 3 dr_schmerzen_w_k_1 any any 0.9 1.0 0 0
sound anim drache.sitzen_get_vorne_mittel 3 dr_schmerzen_m_k_1 any any 0.9 1.0 0 0
sound anim drache.sitzen_get_vorne_mittel 3 dr_schmerzen_m_m_1 any any 0.9 1.0 0 0
sound anim drache.sitzen_get_vorne_mittel 3 dr_schmerzen_m_m_2 any any 0.9 1.0 0 0
sound anim drache.sitzen_get_toetlich 6 dr_schmerzen_s_m_1 any any 0.9 1.0 0 0
sound anim drache.sitzen_get_toetlich 6 dr_schmerzen_s_l_1 any any 0.9 1.0 0 0
sound anim drache.sitzen_peitschen 0 dr_wuetend_s_m_1 any any 0.9 1.0 0 0
sound anim drache.sitzen_vorbeugen 4 dr_angestrengt_s_m_2 any any 0.9 1.0 0 0
sound anim drache.sitzen_zu_pumpe_loop 0 dr_angestrengt_s_m_2 any any 0.9 1.0 0 0

sound anim drache.mama_sitzen_get_hinten_leicht 3 drma_schmerzen_w_m_1 any any 0.9 1.0 0 0
sound anim drache.mama_sitzen_get_hinten_mittel 3 drma_schmerzen_m_k_1 any any 0.9 1.0 0 0
sound anim drache.mama_sitzen_get_hinten_mittel 3 drma_schmerzen_m_l_1 any any 0.9 1.0 0 0
sound anim drache.mama_sitzen_get_hinten_mittel 3 drma_schmerzen_m_m_1 any any 0.9 1.0 0 0
sound anim drache.mama_sitzen_get_vorne_leicht 3 drma_schmerzen_m_k_1 any any 0.9 1.0 0 0
sound anim drache.mama_sitzen_get_vorne_mittel 3 drma_schmerzen_m_k_1 any any 0.9 1.0 0 0
sound anim drache.mama_sitzen_get_vorne_mittel 3 drma_schmerzen_m_l_1 any any 0.9 1.0 0 0
sound anim drache.mama_sitzen_get_vorne_mittel 3 drma_schmerzen_m_m_1 any any 0.9 1.0 0 0
sound anim drache.mama_sitzen_get_toetlich 6 drma_schmerzen_s_l_1 any any 0.9 1.0 0 0
sound anim drache.mama_sitzen_vorbeugen 4 drma_angestrengt_s_m_1 any any 0.9 1.0 0 0


//--------------------------DRACHE01------------------------------

sound anim drache01.schulter_r_ohren 6 ohren any any 0.9 1.0 0 0
sound anim drache01.schulter_r_peitschen 1 fauchen1 any any 0.9 1.0 0 0
sound anim drache01.schulter_r_peitschen 11 schulter_r_peitschen any any 0.9 1.0 0 0
sound anim drache01.schulter_r_speien 15 schulter_r_speien01 any any 0.9 1.0 0 0
sound anim drache01.schulter_r_zu_sitzen 11 schulter_r_zu_sitzen any any 0.9 1.0 0 0
sound anim drache01.sitzen_beissen 4 sitzen_beissen any any 0.9 1.0 0 0
sound anim drache01.sitzen_get_hinten_leicht 3 stehen_get_h_l any any 0.9 1.0 0 0
sound anim drache01.sitzen_ja 3 sitzen_ja any any 0.9 1.0 0 0
sound anim drache01.sitzen_knuddeln 12 sitzen_knuddeln any any 0.9 1.0 0 0
sound anim drache01.sitzen_kratzen 6 sitzen_kratzen any any 0.9 1.0 0 0
sound anim drache01.sitzen_nagen 9 sitzen_nagen any any 0.9 1.0 0 0
sound anim drache01.sitzen_nein 9 sitzen_nein any any 0.9 1.0 0 0
sound anim drache01.sitzen_speien_end 0 speien_end any any 0.9 1.0 0 0
sound anim drache01.sitzen_speien_loop 0 speien_loop any any 0.9 1.0 0 0
sound anim drache01.sitzen_speien_oben 18 speien_oben any any 0.9 1.0 0 0
sound anim drache01.sitzen_speien_start 8 speien_start any any 0.9 1.0 0 0
sound anim drache01.sitzen_umschauen_l 15 ohren any any 0.9 1.0 0 0
sound anim drache01.sitzen_umschauen_r 15 ohren any any 0.9 1.0 0 0
sound anim drache01.sitzen_zu_schulter_r 12 schulter_r_zu_sitzen any any 0.9 1.0 0 0
sound anim drache01.sitzen_zu_stehen 5 sitzen_zu_stehen any any 0.9 1.0 0 0
// stehen_anspringen laut Liste auf 2, aber Aktion ist erst viel spaeter
sound anim drache01.stehen_anspringen 44 stehen_anspringen any any 0.9 1.0 0 0
sound anim drache01.stehen_beissen 6 sitzen_beissen any any 0.9 1.0 0 0
sound anim drache01.stehen_beissen 14 schulter_r_zu_sitzen any any 0.9 1.0 0 0
sound anim drache01.stehen_gehen_end 2 stehen_gehen_end any any 0.9 1.0 0 0
sound anim drache01.stehen_gehen_loop 5 stehen_gehen_loop1 any any 0.9 1.0 0 0
sound anim drache01.stehen_gehen_loop 11 stehen_gehen_loop1 any any 0.9 1.0 0 0
sound anim drache01.stehen_get_hinten_leicht 3 stehen_get_h_l any any 0.9 1.0 0 0
sound anim drache01.stehen_get_hinten_mittel 3 stehen_get_h_m any any 0.9 1.0 0 0
sound anim drache01.stehen_get_hinten_schwer 3 stehen_get_h_s any any 0.9 1.0 0 0
sound anim drache01.stehen_get_vorne_leicht 3 stehen_get_h_l any any 0.9 1.0 0 0
sound anim drache01.stehen_get_vorne_mittel 3 stehen_get_v_m any any 0.9 1.0 0 0
sound anim drache01.stehen_get_vorne_schwer 3 stehen_get_v_s2 any any 0.9 1.0 0 0
sound anim drache01.stehen_get_vorne_schwer 18 stehen_get_v_s1 any any 0.9 1.0 0 0
sound anim drache01.stehen_get_vorne_toetlich 3 stehen_get_v_t1 any any 0.9 1.0 0 0
sound anim drache01.stehen_get_vorne_toetlich 22 stehen_get_v_t2 any any 0.9 1.0 0 0
sound anim drache01.stehen_hoppsen_end 0 stehen_hopsen_end any any 0.9 1.0 0 0
sound anim drache01.stehen_hoppsen_loop 1 stehen_hopsen_loop any any 0.9 1.0 0 0
sound anim drache01.stehen_laufen_end 0 stehen_laufen_end any any 0.9 1.0 0 0
sound anim drache01.stehen_laufen_loop 2 stehen_laufen_loop any any 0.9 1.0 0 0
sound anim drache01.stehen_schweben_end 2 stehen_schweben_end any any 0.9 1.0 0 0
sound anim drache01.stehen_schweben_loop 1 stehen_schweben_loop any any 0.9 1.0 0 0
sound anim drache01.stehen_schweben_oben_end 2 stehen_schweben_end any any 0.9 1.0 0 0
sound anim drache01.stehen_schweben_oben_loop 0 stehen_schweben_oben_loop any any 0.9 1.0 0 0
sound anim drache01.stehen_schweben_oben_start 4 stehen_schweben_start any any 0.9 1.0 0 0
sound anim drache01.stehen_schweben_start 4 stehen_schweben_start any any 0.9 1.0 0 0
sound anim drache01.stehen_schweben_unten_end 2 stehen_schweben_end any any 0.9 1.0 0 0
sound anim drache01.stehen_schweben_unten_loop 0 stehen_schweben_oben_loop any any 0.9 1.0 0 0
sound anim drache01.stehen_schweben_unten_start 4 stehen_schweben_start any any 0.9 1.0 0 0
sound anim drache01.stehen_speien_loop 0 speien_loop any any 0.9 1.0 0 0
sound anim drache01.stehen_trappeln 3 stehen_trappeln any any 0.9 1.0 0 0
sound anim drache01.stehen_umdrehen_l_180 5 stehen_umdrehen_l_180 any any 0.9 1.0 0 0
sound anim drache01.stehen_umdrehen_r_180 5 stehen_umdrehen_l_180 any any 0.9 1.0 0 0
sound anim drache01.stehen_umspringen_l_90 2 stehen_anspringen any any 0.9 1.0 0 0
sound anim drache01.stehen_umspringen_r_90 2 stehen_anspringen any any 0.9 1.0 0 0
sound anim drache01.stehen_wenden_l_180 3 stehen_wenden1 any any 0.9 1.0 0 0
sound anim drache01.stehen_wenden_l_180 18 stehen_wenden2 any any 0.9 1.0 0 0
sound anim drache01.stehen_wenden_r_180 3 stehen_wenden1 any any 0.9 1.0 0 0
sound anim drache01.stehen_wenden_r_180 18 stehen_wenden2 any any 0.9 1.0 0 0
sound anim drache01.stehen_zu_sitzen 10 stehen_zu_sitzen any any 0.9 1.0 0 0



//--------------------------EINRICHTUNG-----------------------------

sound anim tuer_kaserne.oeffnen_a 0 Metalltuer_auf any any 0.9 1.0 0 0
sound anim tuer_kaserne.oeffnen_b 0 Metalltuer_auf any any 0.9 1.0 0 0
sound anim tuer_kaserne.schliessen_a 0 Metalltuer_zu any any 0.9 1.0 0 0
sound anim tuer_kaserne.schliessen_b 0 Metalltuer_zu any any 0.9 1.0 0 0

sound anim holztuer.rechts_oeffnen 0 Metalltuer_auf any any 0.9 1.0 0 0
sound anim holztuer.links_oeffnen 0 Metalltuer_auf any any 0.9 1.0 0 0
sound anim holztuer.rechts_schliessen 0 Metalltuer_zu any any 0.9 1.0 0 0
sound anim holztuer.links_schliessen 0 Metalltuer_zu any any 0.9 1.0 0 0

sound anim steintuer.rechts_oeffnen 0 Steintuer_auf any any 0.9 1.0 0 0
sound anim steintuer.links_oeffnen 0 Steintuer_auf any any 0.9 1.0 0 0
sound anim steintuer.rechts_schliessen 0 Steintuer_zu any any 0.9 1.0 0 0
sound anim steintuer.links_schliessen 0 Steintuer_zu any any 0.9 1.0 0 0

sound anim metalltuer.rechts_oeffnen 0 Metalltuer_auf any any 0.9 1.0 0 0
sound anim metalltuer.links_oeffnen 0 Metalltuer_auf any any 0.9 1.0 0 0
sound anim metalltuer.rechts_schliessen 0 Metalltuer_zu any any 0.9 1.0 0 0
sound anim metalltuer.links_schliessen 0 Metalltuer_zu any any 0.9 1.0 0 0

sound anim kristalltuer.rechts_oeffnen 0 Kristalltuer_auf any any 0.9 1.0 0 0
sound anim kristalltuer.links_oeffnen 0 Kristalltuer_auf any any 0.9 1.0 0 0
sound anim kristalltuer.rechts_schliessen 0 Kristalltuer_zu any any 0.9 1.0 0 0
sound anim kristalltuer.links_schliessen 0 Kristalltuer_zu any any 0.9 1.0 0 0



//--------------------------FENRIS---------------------------------

sound anim fenrir.bad_reden_b_end 13 fe_lava2 any any 0.9 1.0 0 0
sound anim fenrir.bad_reden_b_start 10 fe_lava1 any any 0.9 1.0 0 0
sound anim fenrir.bad_reden_start 5 fe_lava2 any any 0.9 1.0 0 0
sound anim fenrir.bad_waschen 7 fe_haare_waschen any any 0.9 1.0 0 0
sound anim fenrir.betr_umfallen 21 fe_swish_m_k_1 any any 0.9 1.0 0 0
sound anim fenrir.betr_umfallen 28 fe_fallen any any 0.9 1.0 0 0
sound anim fenrir.drehen_ganz 5 fe_swish_m_k_1 any any 0.9 1.0 0 0
sound anim fenrir.drehen_ganz 15 fe_sprung any any 0.9 1.0 0 0
sound anim fenrir.drehen_links 7 fe_schritt1 any any 0.9 1.0 0 0
sound anim fenrir.drehen_links 10 fe_schritt2 any any 0.9 1.0 0 0
sound anim fenrir.drehen_rechts 7 fe_schritt1 any any 0.9 1.0 0 0
sound anim fenrir.drehen_rechts 10 fe_schritt2 any any 0.9 1.0 0 0
sound anim fenrir.fenris_zu_fifi 6 fe_fenris_zu_fiffi any any 0.9 1.0 0 0
sound anim fenrir.fifi_zu_fenris 0 fe_fiffi_zu_fenris any any 0.9 1.0 0 0
sound anim fenrir.fifi_zu_fenris 21 fe_schritt1 any any 0.9 1.0 0 0
sound anim fenrir.fifi_zu_fenris 27 fe_schritt2 any any 0.9 1.0 0 0
sound anim fenrir.gehen_loop 0 fe_schritt2 any any 0.9 1.0 0 0
sound anim fenrir.gehen_loop 9 fe_schritt1 any any 0.9 1.0 0 0
sound anim fenrir.gehen_end 6 fe_schritt2 any any 0.9 1.0 0 0
sound anim fenrir.gehen_zu_stand 6 fe_schritt2 any any 0.9 1.0 0 0
sound anim fenrir.handgeste_a 5 fe_doppelswish1 any any 0.9 1.0 0 0
sound anim fenrir.handgeste_a 6 fe_schritt1 any any 0.9 1.0 0 0
sound anim fenrir.handgeste_a 40 fe_schritt1 any any 0.9 1.0 0 0
sound anim fenrir.hinsetzen 3 fe_schritt2 any any 0.9 1.0 0 0
sound anim fenrir.hinsetzen 6 fe_schritt1 any any 0.9 1.0 0 0
sound anim fenrir.hinsetzen 9 fe_schritt2 any any 0.9 1.0 0 0
sound anim fenrir.hinsetzen 17 fe_setzten any any 0.9 1.0 0 0
sound anim fenrir.hinstellen_sitzen_start 9 fe_schritt2 any any 0.9 1.0 0 0
sound anim fenrir.hinstellen_sitzen_start 23 fe_schritt2 any any 0.9 1.0 0 0
sound anim fenrir.hinstellen_sitzen_end 2 fe_setzten any any 0.9 1.0 0 0
sound anim fenrir.lolli 0 fe_lolli_haare any any 0.9 1.0 0 0
sound anim fenrir.kampf_schlag_a_start 5 fe_swish_m_k_1 any any 0.9 1.0 0 0
sound anim fenrir.kampf_schlag_a_start 7 fe_schlag any any 0.9 1.0 0 0
sound anim fenrir.kampf_schlag_b_start 5 fe_swish_w_k_2 any any 0.9 1.0 0 0
sound anim fenrir.kampf_schlag_b_start 6 fe_schlag any any 0.9 1.0 0 0
sound anim fenrir.kampf_treten_a_start 6 fe_swish_s_k_1 any any 0.9 1.0 0 0
sound anim fenrir.kampf_treten_a_end 5 fe_schritt1 any any 0.9 1.0 0 0
sound anim fenrir.kampf_zu_knie 7 fe_fallen any any 0.9 1.0 0 0
sound anim fenrir.kampf_zu_stand 4 fe_schritt2 any any 0.9 1.0 0 0
sound anim fenrir.knie_schlag_a_start 5 fe_swish_w_k_3 any any 0.9 1.0 0 0
sound anim fenrir.knie_schlag_a_start 6 fe_schlag any any 0.9 1.0 0 0
sound anim fenrir.knie_schuetteln 5 fe_swish_m_k_2 any any 0.9 1.0 0 0
sound anim fenrir.knie_schuetteln 8 fe_swish_m_k_2 any any 0.9 1.0 0 0
sound anim fenrir.knie_schuetteln 11 fe_swish_m_k_1 any any 0.9 1.0 0 0
sound anim fenrir.knie_umfallen 6 fe_fallen any any 0.9 1.0 0 0
sound anim fenrir.quietschewiggle_loop 0 fe_quitschewiggle any any 0.9 1.0 0 0
sound anim fenrir.quietschewiggle_end 0 fe_swish_w_k_1 any any 0.9 1.0 0 0
sound anim fenrir.reden_fernseher_a_zu_b 4 fe_schritt1 any any 0.9 1.0 0 0
sound anim fenrir.reden_fernseher_a_zu_b 7 fe_schritt2 any any 0.9 1.0 0 0
sound anim fenrir.reden_fernseher_b_end 8 fe_swish_w_k_1 any any 0.9 1.0 0 0
sound anim fenrir.schreck_a 5 fe_swish_s_k_1 any any 0.9 1.0 0 0
sound anim fenrir.schreck_a 7 fe_sprung any any 0.9 1.0 0 0
sound anim fenrir.schreck_a 17 fe_swish_w_k_3 any any 0.9 1.0 0 0
sound anim fenrir.schreck_a 19 fe_schritt2 any any 0.9 1.0 0 0
sound anim fenrir.schreck_b 24 fe_swish_m_k_2 any any 0.9 1.0 0 0
sound anim fenrir.schreck_b 29 fe_sprung any any 0.9 1.0 0 0
sound anim fenrir.sitzen_fausttisch 10 fe_fausttisch any any 0.9 1.0 0 0
sound anim fenrir.sitzen_haare 10 fe_haare any any 0.9 1.0 0 0
sound anim fenrir.sitzen_haare_start 10 fe_haare any any 0.9 1.0 0 0
sound anim fenrir.sitzen_haare_start 25 fe_feuer2 any any 0.9 1.0 0 0
sound anim fenrir.sitzen_haare_start 25 fe_feuer2 any any 0.9 1.0 0 0
sound anim fenrir.sitzen_pflanzen_start 0 fe_pflanzen_start any any 1 1.0 0 0
sound anim fenrir.sitzen_schlagen 8 fe_swish_s_k_1 any any 0.9 1.0 0 0
sound anim fenrir.sitzen_zu_gehen 26 fe_swish_s_k_1 any any 0.9 1.0 0 0
sound anim fenrir.sitzen_zu_gehen 29 fe_sprung any any 0.9 1.0 0 0
sound anim fenrir.stand_zu_kampf 6 fe_sprung any any 0.9 1.0 0 0
sound anim fenrir.thron_verzweifeln_start 8 fe_swish_w_k_2 any any 0.9 1.0 0 0
sound anim fenrir.thron_verzweifeln 8 fe_swish_w_k_2 any any 0.9 1.0 0 0
sound anim fenrir.trinken_end_mit 6 fe_fausttisch any any 0.9 1.0 0 0

//--------------------------FENRIS-INGMAME----------------------

sound anim fenrir.betr_umfallen 6 fe_erschoepft_m_k_1 any any 0.9 1.0 0 0
sound anim fenrir.lolli 0 fe_jaulend_s_m_1 any any 0.9 1.0 0 0
sound anim fenrir.kampf_schlag_a_start 0 fe_schreiend_m_m_2 any any 0.9 1.0 0 0
sound anim fenrir.kampf_schlag_a_start 7 fe_schreiend_s_m_1 any any 0.9 1.0 0 0
sound anim fenrir.kampf_schlag_b_start 0 fe_schreiend_m_m_2 any any 0.9 1.0 0 0
sound anim fenrir.kampf_schlag_b_start 6 fe_schreiend_s_m_1 any any 0.9 1.0 0 0
sound anim fenrir.kampf_treten_a_start 0 fe_schreiend_m_m_1 any any 0.9 1.0 0 0
sound anim fenrir.kampf_treten_a_start 3 fe_schreiend_s_m_1 any any 0.9 1.0 0 0
sound anim fenrir.kampf_treten_a_start 8 fe_lachend_s_m_1 any any 0.9 1.0 0 0
sound anim fenrir.kampf_zu_knie 4 fe_schmerzen_s_k_1 any any 0.9 1.0 0 0
sound anim fenrir.knie_bruellen 5 fe_jaulend_s_m_1 any any 0.9 1.0 0 0
sound anim fenrir.knie_schlag_a_start 0 fe_schreiend_m_m_1 any any 0.9 1.0 0 0
sound anim fenrir.knie_schlag_a_start 6 fe_schreiend_s_m_1 any any 0.9 1.0 0 0
sound anim fenrir.knie_umfallen 1 fe_erschoepft_s_m_4 any any 0.9 1.0 0 0
//sound anim fenrir.sitzen_fausttisch 0 fe_grummeln_s_l_2 any any 0.9 1.0 0 0
//sound anim fenrir.sitzen_pflanzen_start 9 fe_schreiend_s_m_1 any any 0.9 1.0 0 0
//sound anim fenrir.sitzen_pflanzen_start 20 fe_schmerzen_s_l_1 any any 0.9 1.0 0 0
//sound anim fenrir.sitzen_pflanzen_start 49 fe_leidend_m_l_1 any any 0.9 1.0 0 0
sound anim fenrir.sitzen_schlagen 8 fe_schreiend_m_m_2 any any 0.9 1.0 0 0
sound anim fenrir.thron_verzweifeln_start 0 fe_grummeln_s_l_1 any any 0.9 1.0 0 0
sound anim fenrir.thron_verzweifeln_start 15 fe_leidend_m_l_2 any any 0.9 1.0 0 0
sound anim fenrir.verzweifeln 0 fe_jaulend_s_m_1 any any 0.9 1.0 0 0
sound anim fenrir.verzweifeln 31 fe_leidend_m_l_1 any any 0.9 1.0 0 0




//--------------------------FIFI--------------------------------

sound anim fifi.bein_heben 0 fifi_bein_heben any any 0.9 1.0 0 0
sound anim fifi.bein_heben_g 0 fifi_bein_heben any any 0.9 1.0 0 0
sound anim fifi.bein_heben_g 12 fifi_gleip_ruhig_a any any 0.9 1.0 0 0
//sound anim fifi.gehen_loop 2 fifi_geh_step1 any any 0.9 1.0 0 0
//sound anim fifi.gehen_loop 4 fifi_geh_step2 any any 0.9 1.0 0 0
//sound anim fifi.gehen_loop 6 fifi_geh_step1 any any 0.9 1.0 0 0
//sound anim fifi.gehen_loop 7 fifi_geh_step2 any any 0.9 1.0 0 0
//sound anim fifi.gehen_loop_g 2 fifi_geh_step1 any any 0.9 1.0 0 0
//sound anim fifi.gehen_loop_g 4 fifi_geh_step2 any any 0.9 1.0 0 0
//sound anim fifi.gehen_loop_g 6 fifi_geh_step1 any any 0.9 1.0 0 0
//sound anim fifi.gehen_loop_g 7 fifi_geh_step2 any any 0.9 1.0 0 0
sound anim fifi.hopsen 6 fifi_hopsen any any 0.9 1.0 0 0
sound anim fifi.hopsen_g 6 fifi_hopsen any any 0.9 1.0 0 0
sound anim fifi.hopsen_g 6 fifi_gleip_kurz_c any any 0.9 1.0 0 0
//sound anim fifi.laufen_loop 1 fifi_laufen_loop any any 0.9 1.0 0 0
//sound anim fifi.laufen_loop_g 1 fifi_laufen_loop any any 0.9 1.0 0 0
//sound anim fifi.laufen_loop_g 2 fifi_gleip_kurz_a any any 0.9 1.0 0 0
sound anim fifi.odin_arm 4 fifi_odin_arm any any 0.9 1.0 0 0
//sound anim fifi.traben_loop 2 fifi_geh_trab1 any any 0.9 1.0 0 0
//sound anim fifi.traben_loop 7 fifi_geh_trab2 any any 0.9 1.0 0 0
//sound anim fifi.traben_loop_g 2 fifi_geh_trab1 any any 0.9 1.0 0 0
//sound anim fifi.traben_loop_g 7 fifi_geh_trab2 any any 0.9 1.0 0 0
sound anim fifi.traben_loop_g 2 fifi_gleip_kurz_d any any 0.9 1.0 0 0
sound anim fifi.traben_loop_g 7 fifi_gleip_kurz_b any any 0.9 1.0 0 0


//--------------------------HAMSTER-----------------------------

sound anim hamster.drehen_links_90 11 ham_drehen any any 0.4 1.0 0 0
sound anim hamster.drehen_rechts_90 11 ham_drehen any any 0.4 1.0 0 0
sound anim hamster.hoppeln_loop 0 ham_hoppeln_l any any 0.4 1.0 0 0
sound anim hamster.laufen_loop 0 ham_laufen_l any any 0.4 1.0 0 0
sound anim hamster.laufrad_loop 1 ham_laufrad any any 0.4 1.0 0 0
sound anim hamster.maennchen 5 ham_maennchen any any 0.4 1.0 0 0
sound anim hamster.putzen_loop 5 ham_putzen any any 0.4 1.0 0 0
sound anim hamster.schlafen_start 2 ham_schlaf_s any any 0.4 1.0 0 0
sound anim hamster.schlafen_end 2 ham_schlaf_e any any 0.4 1.0 0 0
sound anim hamster.sterben 6 ham_sterben any any 0.4 1.0 0 0


//-------------------------KRAKE--------------------------------

sound anim krake.schlag_fuchteln 0 kr_glitschatmo any any 0.9 1.0 0 0
sound anim krake.schlag_fuchteln 25 kr_glitschatmo any any 0.9 1.0 0 0
sound anim krake.schlag_fuchteln 8 kr_swish3 any any 0.9 1.0 0 0
sound anim krake.schlag_fuchteln 30 kr_swish1 any any 0.9 1.0 0 0
sound anim krake.schlag_hinten 0 kr_glitschatmo any any 0.9 1.0 0 0
sound anim krake.schlag_hinten 25 kr_glitschatmo any any 0.9 1.0 0 0
sound anim krake.schlag_hinten 1 kr_swish7 any any 0.9 1.0 0 0
sound anim krake.schlag_hinten 7 kr_swish5 any any 0.9 1.0 0 0
sound anim krake.schlag_schalter 0 kr_glitschatmo any any 0.9 1.0 0 0
sound anim krake.schlag_schalter 25 kr_glitschatmo any any 0.9 1.0 0 0
sound anim krake.schlag_schalter 0 kr_swish4 any any 0.9 1.0 0 0
sound anim krake.schlag_schalter 16 kr_swish5 any any 0.9 1.0 0 0
sound anim krake.schlag_schalter 37 kr_swish4 any any 0.9 1.0 0 0
sound anim krake.schlag_treppenbucht 0 kr_glitschatmo any any 0.9 1.0 0 0
sound anim krake.schlag_treppenbucht 25 kr_glitschatmo any any 0.9 1.0 0 0
sound anim krake.schlag_treppenbucht 11 kr_swish1 any any 0.9 1.0 0 0
sound anim krake.schlag_treppenbucht 12 kr_glitsch any any 0.9 1.0 0 0
sound anim krake.schlag_vorne 0 kr_glitschatmo any any 0.9 1.0 0 0
sound anim krake.schlag_vorne 25 kr_glitschatmo any any 0.9 1.0 0 0
sound anim krake.schlag_vorne 12 kr_swish2 any any 0.9 1.0 0 0
sound anim krake.schlag_vorne 29 kr_swish6 any any 0.9 1.0 0 0
sound anim krake.schlag_vorne 34 kr_swish3 any any 0.9 1.0 0 0
sound anim krake.tasten_gold 0 kr_glitschatmo any any 0.9 1.0 0 0
sound anim krake.tasten_gold 25 kr_glitschatmo any any 0.9 1.0 0 0
sound anim krake.tasten_gold 50 kr_glitschatmo any any 0.9 1.0 0 0
sound anim krake.tasten_gold 75 kr_glitschatmo any any 0.9 1.0 0 0
sound anim krake.tasten_gold 36 kr_tasten_gold any any 0.9 1.0 0 0
sound anim krake.tasten_gold 4 kr_swish1 any any 0.9 1.0 0 0
sound anim krake.tasten_gold 89 kr_swish2 any any 0.9 1.0 0 0
sound anim krake.tod_durch_kronleuchter 0 kr_glitschatmo any any 0.9 1.0 0 0
sound anim krake.tod_durch_kronleuchter 25 kr_glitschatmo any any 0.9 1.0 0 0
sound anim krake.tod_durch_kronleuchter 50 kr_glitschatmo any any 0.9 1.0 0 0
sound anim krake.tod_durch_kronleuchter 75 kr_glitschatmo any any 0.9 1.0 0 0
sound anim krake.tod_durch_kronleuchter 1 kr_tod_durch_kronleuchter1 any any 2 1.0 0 0
sound anim krake.tod_durch_kronleuchter 38 kr_tod_durch_kronleuchter2 any any 2 1.0 0 0
sound anim krake.tod_durch_kronleuchter 89 kr_tod_durch_kronleuchter3 any any 2 1.0 0 0
sound anim krake.treffer 0 kr_glitschatmo any any 0.9 1.0 0 0
sound anim krake.treffer 25 kr_glitschatmo any any 0.9 1.0 0 0
sound anim krake.treffer 3 kr_treffer any any 0.9 1.0 0 0
sound anim krake.treppenbucht_schalter 0 kr_glitschatmo any any 0.9 1.0 0 0
sound anim krake.treppenbucht_schalter 25 kr_glitschatmo any any 0.9 1.0 0 0
sound anim krake.treppenbucht_schalter 43 kr_swish1 any any 0.9 1.0 0 0
sound anim krake.warten 0 kr_glitschatmo any any 0.9 1.0 0 0
sound anim krake.warten 25 kr_glitschatmo any any 0.9 1.0 0 0



//-------------------------LANDSCHAFT-------------------------

sound animnoseq kette.rausziehen 1 kette_rausziehen any any 0.9 1.0 0 0
sound anim lava_fluss_a.standard 0 lava_fliessen_a any any 0.9 1.0 0 0
sound anim lava_fluss_b.standard 0 lava_fliessen_b any any 0.9 1.0 0 0
sound anim lava_fluss_c.standard 0 lava_fliessen_c any any 0.9 1.0 0 0
sound anim lava_tuer.oeffnen 0 tuer_oeffnen any any 0.9 1.0 0 0
sound anim lava_tuer.schliessen 0 tuer_schliessen any any 0.9 1.0 0 0
sound anim riesentor.oeffnen 2 riesentor_auf any any 0.9 1.0 0 0
sound anim riesentor.schliessen 1 riesentor_zu any any 0.9 1.0 0 0
sound anim kris_maschine.betrieb 0 brainmasch_heil_loop any any 0.8 1.0 0 0
sound anim dampflore.fahren 0 lore_fahren any any 0.9 1.0 0 0
sound anim titanic_kolben.stampfen 0 fe_schritt2 any any 0.9 0.8 0 0

sound anim fresspflanze.get_loop 3 schwert_o_g_t any any 0.5 1.0 0 0
sound anim alienpflanze.get_loop 3 schwert_o_g_t any any 0.5 1.0 0 0

//-------------------------WUKER-----------------------------

sound anim wuker.beissen_a 0 w_beissen_a any any 0.92 1.0 0 0
sound anim wuker.beissen_mitte 0 w_beissen_mitte any any 0.92 1.0 0 0
sound anim wuker.boxen_oben 2 w_boxen any any 0.9 1.0 0 0

sound anim wuker.drehen_ganz 2 w_hopsen_a any any 0.82 1.0 0 0
sound anim wuker.drehen_links_a 2 w_drehen_links any any 0.82 1.0 0 0
sound anim wuker.drehen_rechts_a 4 w_drehen_rechts any any 0.82 1.0 0 0

sound anim wuker.entdecken_a_start 0 WV_tired_a any any 0.9 1.0 0 0

sound anim wuker.fliehen_a_end 0 w_fliehen_e any any 0.82 1.0 0 0
sound anim wuker.fliehen_a_loop 0 w_fliehen_l any any 0.84 1.0 0 0
sound anim wuker.fliehen_a_start 0 w_fliehen_s any any 0.82 1.0 0 0
sound anim wuker.fressen 0 w_fressen_a any any 0.92 1.0 0 0

sound anim wuker.gehen_end 0 w_gehen_e any any 0.80 1.0 0 0
sound anim wuker.gehen_loop 0 w_gehen_l any any 0.82 1.0 0 0
sound anim wuker.gehen_start 0 w_gehen_s any any 0.80 1.0 0 0

sound anim wuker.hinten_get_leicht 3 w_h_g_l any any 0.9 1.0 0 0
sound anim wuker.hinten_get_mittel 3 w_h_g_m any any 0.9 1.0 0 0
sound anim wuker.hinten_get_schwer 1 w_h_g_s any any 0.9 1.0 0 0
sound anim wuker.hinten_get_tot 1 w_h_g_t any any 0.9 1.0 0 0

sound anim wuker.hopser_a 2 w_hopsen_a any any 0.84 1.0 0 0

sound anim wuker.kletter_hoch 4 w_kletter_hoch any any 0.9 1.0 0 0
sound anim wuker.kletter_links 6 w_kletter_links any any 0.9 1.0 0 0
sound anim wuker.kletter_rechts 6 w_kletter_links any any 0.9 1.0 0 0
sound anim wuker.kletter_runter 4 w_kletter_runter any any 0.9 1.0 0 0
sound anim wuker.kletterstand_zu_stand 0 w_kletterst_stand any any 0.9 1.0 0 0

sound anim wuker.kopf_drehen_links_a 0 w_kopf_drehen_s any any 0.84 1.0 0 0
sound anim wuker.kopf_drehen_links_a 15 w_kopf_drehen_e any any 0.82 1.0 0 0
sound anim wuker.kopf_drehen_rechts_a 3 w_kopf_drehen_s any any 0.84 1.0 0 0
sound anim wuker.kopf_drehen_rechts_a 15 w_kopf_drehen_e any any 0.82 1.0 0 0

sound anim wuker.kratzen_a 3 w_kratzen_a any any 0.88 1.0 0 0
sound anim wuker.kratzen_b 1 w_kratzen_b any any 0.88 1.0 0 0
sound anim wuker.kratzen_c_loop 2 w_kratzen_c_s any any 0.9 1.0 0 0
sound anim wuker.kratzen_c_loop 7 w_kratzen_c_e any any 0.9 1.0 0 0

sound anim wuker.lauf_a_loop 0 w_laufen_l any any 0.82 1.0 0 0

sound anim wuker.mitte_get_leicht 3 w_m_g_l any any 0.9 1.0 0 0
sound anim wuker.mitte_get_leicht 4 WV_lowrage_b any any 0.9 1.0 0 0
sound anim wuker.mitte_get_mittel 3 w_m_g_m any any 0.9 1.0 0 0
sound anim wuker.mitte_get_mittel 4 WV_suffer_a any any 0.9 1.0 0 0
sound anim wuker.mitte_get_schwer 2 w_m_g_s any any 0.9 1.0 0 0
sound anim wuker.mitte_get_schwer 3 WV_suffer_b any any 0.9 1.0 0 0
sound anim wuker.mitte_get_tot 2 w_m_g_t any any 0.9 1.0 0 0
sound anim wuker.mitte_get_tot 0 WV_normal_a any any 0.9 1.0 0 0

sound anim wuker.nachspringen_a 7 w_nachspringen_a any any 0.85 1.0 0 0
sound anim wuker.nachspringen_a 22 w_nachspringen_a_end any any 0.85 1.0 0 0
sound anim wuker.nachspringen_b 7 w_hopsen_a any any 0.9 1.0 0 0

sound anim wuker.oben_get_leicht 3 w_m_g_l any any 0.9 1.0 0 0
sound anim wuker.oben_get_leicht 4 WV_aggression_a any any 0.9 1.0 0 0
sound anim wuker.oben_get_mittel 3 w_o_g_m any any 0.9 1.0 0 0
sound anim wuker.oben_get_mittel 4 WV_aggression_b any any 0.9 1.0 0 0
sound anim wuker.oben_get_schwer 2 w_o_g_s any any 0.9 1.0 0 0
sound anim wuker.oben_get_schwer 0 WV_aggression_c any any 0.9 1.0 0 0
sound anim wuker.oben_get_tot 2 w_o_g_t any any 0.9 1.0 0 0
sound anim wuker.oben_get_tot 0 WV_normal_a any any 0.9 1.0 0 0

sound anim wuker.schlagen_oben 2 w_schlagen any any 0.9 1.0 0 0

sound anim wuker.schnueffeln_b_loop 0 w_schnueffel any any 0.9 1.0 0 0
sound anim wuker.schnueffeln_entdecken_a 3 w_schnueffel_entd any any 0.9 1.0 0 0
sound anim wuker.schnueffeln_linksrum 0 w_schnueffel_links any any 0.9 1.0 0 0
sound anim wuker.schnueffeln_rechtsrum 0 w_schnueffel_rechts any any 0.9 1.0 0 0
sound anim wuker.schuetteln_a 0 w_schuetteln any any 0.9 1.0 0 0

sound anim wuker.unten_get_leicht 3 w_h_g_l any any 0.9 1.0 0 0
sound anim wuker.unten_get_leicht 4 WV_tired_a any any 0.9 1.0 0 0
sound anim wuker.unten_get_mittel 3 w_h_g_m any any 0.9 1.0 0 0
sound anim wuker.unten_get_mittel 2 w_schlagen any any 0.9 1.0 0 0
sound anim wuker.unten_get_schwer 2 w_h_g_s any any 0.9 1.0 0 0
sound anim wuker.unten_get_schwer 2 WV_suffer_a any any 0.9 1.0 0 0
sound anim wuker.unten_get_tot 2 w_h_g_t any any 0.9 1.0 0 0

sound anim wuker.versteinert_a 0 w_versteinert any any 0.93 1.0 0 0

//-----------------------PRODUKTIONSSTÄTTEN--------------------


sound anim bordell.bett_anim 4 bett_anim any any 0.9 1.0 0 0
sound anim bordell.oeffnen 1 bordell_oeffnen any any 0.9 1.0 0 0
sound anim bordell.schliessen 1 bordell_schliessen any any 0.9 1.0 0 0
sound animnoseq disco.anim 0 Disco any any 0.9 1.0 0 0
sound anim dampfhammer.ani 4 Dampfhammer any any 0.9 1.0 0 0
sound anim dampfmaschine.anim 0 Dampfmaschine any any 0.9 1.0 0 0
sound anim dreherei.anim 0 Dreherei any any 0.9 1.0 0 0
sound anim feuerstelle.brennt 0 Feuerstelle_1 any any 0.9 1.0 0 0
sound anim feuerstelle.brennt 0 Feuerstelle_2 any any 0.9 1.0 0 0
sound anim feuerstelle.brennt 0 Feuerstelle_3 any any 0.9 1.0 0 0
sound anim feuerstelle.brennt 0 Feuerstelle_4 any any 0.9 1.0 0 0
sound anim feuerstelle.brennt 0 Feuerstelle_5 any any 0.9 1.0 0 0
sound anim feuerstelle.brennt 0 Feuerstelle_6 any any 0.9 1.0 0 0
sound anim feuerstelle.brennt 0 Feuerstelle_7 any any 0.9 1.0 0 0
sound anim feuerstelle.brennt 0 Feuerstelle_8 any any 0.9 1.0 0 0
sound anim hochofen.anim 0 hochofen any any 0.9 1.0 0 0
sound anim hochofen.rad_dreh 0 HO_rad_drehen any any 0.9 1.0 0 0
sound anim krankenhaus.tisch_hoch 0 KH_Tisch any any 0.9 1.0 0 0
sound anim krankenhaus.tisch_runter 0 KH_Tisch any any 0.9 1.0 0 0
sound anim krankenhaus.instr_rollrein 5 KH_rollraus any any 0.9 1.0 0 0
sound anim krankenhaus.instr_rollraus 5 KH_rollraus any any 0.9 1.0 0 0
sound anim kristallschmiede.anim 1 Kristallschmiede any any 0.9 1.0 0 0
sound anim moebeltischlerei.anim 0 Moebeltischlerei any any 0.9 1.0 0 0
sound anim reaktor.anim 0 Reaktor any any 0.9 1.0 0 0
sound anim saegewerk.saegen 0 Saegewerk any any 0.9 1.0 0 0
sound anim schleiferei.anim 0 Schleiferei any any 0.9 1.0 0 0
sound anim schmelze.ani 0 schmelze any any 0.9 1.0 0 0
sound anim tempel.ausfahren 0 tempel_ausfahr any any 0.9 1.0 0 0
sound anim tempel.einfahren 9 tempel_einfahr any any 0.9 1.0 0 0
sound anim tempel.einschalt 0 tempel_einschalt any any 0.9 1.0 0 0
sound anim tempel.pendeln 5 tempel_pendel any any 0.9 1.0 0 0
sound anim titanic_pumpe.anim 0 Dampfmaschine any any 0.9 0.5 0 0
sound anim waffenfabrik.anim 0 waffenfabrik any any 0.9 1.0 0 0

//-----------------------PRODUKTIONSSTÄTTEN--------------------

sound anim riesenelfe.blitze_loop 0 flammenwerfer_a any any 1 1.0 0 0
sound anim riesenelfe.blitze_loop 0 flammenwerfer_b any any 1 1.0 0 0
sound anim riesenelfe.blitze_loop 0 flammenwerfer_c any any 1 1.0 0 0
sound anim riesenelfe.blitze_start 8 flammenwerfer_start any any 1 1.0 0 0
sound anim riesenelfe.blitze_end 0 flammenwerfer_stop any any 1 1.0 0 0



}


//--------------------------FEUER,PILZ----------------------


//sound anim  feuerstelle.brennt 0 snd_step01  any any 0.8 1.0 0 0
//sound anim  feuerstelle.brennt 0 snd_step112 any any 0.9 1.0 0 0
//sound anim  feuerstelle.brennt 0 snd_step113 any any 0.9 1.0 0 0
//sound anim  feuerstelle.brennt 0 snd_step114 any any 0.9 1.0 0 0

//sound anim  pilz.umfallen 1 snd_step30  any any 0.9 1.0 0 0
//sound anim  pilz.umfallen 6 snd_step31  any any 0.9 1.0 0 0

//----------------------------HAMSTER,RAUPE------------------

//sound anim hamster.laufen_loop     5 snd_step92 any any 1.0 1.0 0 0
//sound anim hamster.laufen_loop   12 snd_step93 any any 1.0 1.0 0 0

//sound anim raupe.kriechen   3 snd_step94 any any 1.0 1.0 0 0
//sound anim raupe.kriechen   3 snd_step95 any any 1.0 1.0 0 0
//sound anim raupe.kriechen   4 snd_step96 any any 1.0 1.0 0 0



