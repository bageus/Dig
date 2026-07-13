// Partikelquelle am Zwerg:
//
// 4	- Zwerg brennt
// 5	- Zwerg qualmt (beim oder nach dem Brennen)
// 8	- Partikel beim Pilze fällen
// 9	- Unverwundbarkeitstrank

// *******************************************************************
// Stehen, Laufen, Klettern und Drehen
// *******************************************************************

// ----------- stehen -----------

set_class_anim standstill	mann.standard

set_class_anim standloopa			mann.stand_anim_a
set_class_anim standloopb			mann.stand_anim_b
set_class_anim standloopc			mann.stand_anim_c
set_class_anim standloopd			mann.stand_anim_d



// ---------- normal ------------

set_class_anim walkstart			mann.gehen_start
set_class_anim walkloop				mann.gehen_loop
set_class_anim walkwaveloop			mann.gehen_gruessen
set_class_anim walkstop				mann.gehen_end
set_class_anim walkback				mann.gehen_zurueck

set_class_anim turn180right			mann.drehen_ganz
set_class_anim turn180left			mann.drehen_ganz_links
set_class_anim turnright			mann.drehen_rechts
set_class_anim turnleft				mann.drehen_links

set_class_anim rotateright			mann.drehen_rechts
set_class_anim rotateleft			mann.drehen_links

set_class_anim climbup				mann.kletter_hoch_loop
set_class_anim climbdown			mann.kletter_runter_loop
set_class_anim climbright			mann.kletter_rechts_loop
set_class_anim climbleft			mann.kletter_links_loop

set_class_anim ladderclimbup		mann.leiter_hoch_loop
set_class_anim ladderclimbdown		mann.leiter_runter_loop

set_class_anim ladderupstart		mann.leiter_hoch_start
set_class_anim ladderuploop			mann.leiter_hoch_loop

set_class_anim ladderdownstart		mann.leiter_runter_start
set_class_anim ladderdownloop		mann.leiter_runter_loop
set_class_anim ladderdownend		mann.leiter_runter_end

set_class_anim ladderstill			mann.leiter_stand
set_class_anim ladderend			mann.leiter_zu_boden

set_class_anim climbtostand			mann.kletterstand_zu_stand
set_class_anim standtoclimb			mann.stand_zu_kletterstand
set_class_anim climbstill			mann.kletterstand
set_class_anim climbstillani		mann.kletterstand_anim

// ---------- schnell -----------
set_class_anim walkfaststart		mann.laufen_schnell_start
set_class_anim walkfastloop			mann.laufen_schnell_loop
set_class_anim walkfastwaveloop		mann.gehen_fit_gruessen
set_class_anim walkfaststop			mann.laufen_schnell_end

set_class_anim climbupfast			mann.kletter_hoch_schnell
set_class_anim climbdownfast		mann.kletter_runter_schnell
set_class_anim climbrightfast		mann.kletter_rechts_schnell
set_class_anim climbleftfast		mann.kletter_links_schnell

// ------------ fit -------------

set_class_anim walkfitstart			mann.gehen_fit_start
set_class_anim walkfitloop			mann.gehen_fit_loop
set_class_anim walkfitwaveloop		mann.gehen_fit_gruessen
set_class_anim walkfitstop			mann.gehen_fit_end

set_class_anim rotaterightfit		mann.drehen_rechts_fit
set_class_anim rotateleftfit		mann.drehen_links_fit

// ----------- muede ------------

set_class_anim walktiredstart		mann.gehen_muede_start
set_class_anim walktiredloop		mann.gehen_muede_loop
set_class_anim walktiredwaveloop	mann.gehen_muede_gruessen
set_class_anim walktiredstop		mann.gehen_muede_end

set_class_anim rotaterighttired		mann.drehen_rechts_muede
set_class_anim rotatelefttired		mann.drehen_links_muede

set_class_anim climbuptired			mann.kletter_hoch_loop
set_class_anim climbdowntired		mann.kletter_runter_loop
set_class_anim climbrighttired		mann.kletter_rechts_loop
set_class_anim climblefttired		mann.kletter_links_loop

// ---------- fliehen -----------

set_class_anim fleestart			mann.fliehen_start
set_class_anim fleeloop				mann.fliehen_loop
set_class_anim fleestop				mann.fliehen_end

// --------- schleichen ---------

set_class_anim sneakstart			mann.schleichen_start
set_class_anim sneakloop			mann.schleichen_loop
// set_class_anim sneakwaveloop		mann.gehen_gruessen
set_class_anim sneakstop			mann.schleichen_end

// --------- betrunken ----------

set_class_anim walkdrunkstart		mann.gehen_betrunken_start
set_class_anim walkdrunkloop		mann.gehen_betrunken_loop
set_class_anim walkdrunkstop		mann.gehen_betrunken_end

// --------- unter Wasser -------

set_class_anim walkunderwaterstart	mann.tauchen_start
set_class_anim walkunderwaterloop	mann.tauchen_loop
set_class_anim walkunderwaterstop	mann.tauchen_end

// ---------- Zombie ------------

set_class_anim walkzombiestart		mann.gehen_zombie_start
set_class_anim walkzombieloop		mann.gehen_zombie_loop
set_class_anim walkzombiestop		mann.gehen_zombie_end

// ----------- kungfu -----------

set_class_anim rotaterightkungfu	mann.kungfu_drehen_ganz_a
set_class_anim rotateleftkungfu		mann.kungfu_drehen_ganz_b
set_class_anim turn180rightkungfu	mann.kungfu_drehen_ganz_a
set_class_anim turn180leftkungfu	mann.kungfu_drehen_ganz_b
set_class_anim standstillkungfu		mann.kungfu_standanim

// ------- hamsterreiten --------

set_class_anim ridegeton			mann.hamster_aufsteigen
set_class_anim ridegetoff			mann.hamster_absteigen
set_class_anim ridestart			mann.hamster_start
set_class_anim rideloop				mann.hamster_loop
set_class_anim ridestop				mann.hamster_end

set_class_anim rideturnleft			mann.hamster_drehen_links
set_class_anim rideturnright		mann.hamster_drehen_rechts
set_class_anim ridestand			mann.hamster_sitzen_stand
set_class_anim ridestandloop		mann.hamster_sitzen_loop
set_class_anim rideturn180			mann.hamster_drehen_ganz

// --------- Hoverboard ---------

set_class_anim boardgeton			mann.board_aufsteigen
set_class_anim boardgetoff			mann.board_absteigen
set_class_anim boardstart			mann.board_start
set_class_anim boardloop			mann.board_loop
set_class_anim boardstop			mann.board_end
set_class_anim boardstill			mann.board_stand
set_class_anim boardturn180			mann.board_drehen_ganz

// --------- schwimmen ----------

set_class_anim swimstart			mann.schwimmen_start
set_class_anim swimloop				mann.schwimmen_loop
set_class_anim swimstop				mann.schwimmen_end
set_class_anim swimup				mann.schwimmen_hoch
set_class_anim swimdown				mann.schwimmen_runter
set_class_anim swimwallstill		mann.schwimmen_wand_stand
set_class_anim swimwalldrown		mann.schwimmen_wand_ertrinken
set_class_anim swimdrown			mann.ertrinken

// ----------- fallen ----------------

set_class_anim falldown             mann.fallen_loop
set_class_anim falldownhit          mann.fallen_end_aufstehen
set_class_anim falldowndead         mann.fallen_end_tot

// ----------- Kiste ------------

set_class_anim standstillwalkbox	mann.kiste_stand

set_class_anim walkboxstart			mann.gehen_kiste_start
set_class_anim walkboxloop			mann.gehen_kiste_loop
set_class_anim walkboxstop			mann.gehen_kiste_end

set_class_anim walkboxturn180right  mann.kiste_drehen_rechts	;// für walk - Action
set_class_anim walkboxturn180left   mann.kiste_drehen_links
set_class_anim walkboxturnright  	mann.kiste_drehen_rechts
set_class_anim walkboxturnleft   	mann.kiste_drehen_links
set_class_anim walkboxstandstill	mann.kiste_stand

set_class_anim turn180rightwalkbox	mann.kiste_drehen_rechts	;// für rotate - Action
set_class_anim turn180leftwalkbox	mann.kiste_drehen_links
set_class_anim turnrightwalkbox	  	mann.kiste_drehen_rechts
set_class_anim turnleftwalkbox	   	mann.kiste_drehen_links
set_class_anim rotateleftwalkbox    mann.kiste_drehen_links
set_class_anim rotaterightwalkbox   mann.kiste_drehen_rechts

// ----------- Kiepe ------------

set_class_anim walkpannierstart		mann.gehen_kiepe_start
set_class_anim walkpannierloop		mann.gehen_kiepe_loop
set_class_anim walkpannierstop		mann.gehen_kiepe_end

set_class_anim climbuppannier		mann.kletter_hoch_loop
set_class_anim climbdownpannier		mann.kletter_runter_loop
set_class_anim climbrightpannier	mann.kletter_rechts_loop
set_class_anim climbleftpannier		mann.kletter_links_loop

// -------- Leiter graben -------- (hä?)

set_class_anim ladderdigleftstart	mann.leiter_linksbuddel_start
set_class_anim ladderdigleftloop	mann.leiter_linksbuddel_loop
set_class_anim ladderdigleftstop	mann.leiter_linksbuddel_end

set_class_anim ladderdigrightstart	mann.leiter_rechtsbuddel_start
set_class_anim ladderdigrightloop	mann.leiter_rechtsbuddel_loop
set_class_anim ladderdigrightstop	mann.leiter_rechtsbuddel_end


// *******************************************************************
// Allgemeine Animationen wie Aufheben
// *******************************************************************

// ------------- Hut ---------------
set_class_anim hatofhead			mann.hut_ab_kopf
set_class_anim hatofhand			mann.hut_ab_hand
set_class_anim hatofgone			mann.hut_ab_weg
set_class_anim hatonhead			mann.hut_auf_kopf
set_class_anim hatonhand			mann.hut_auf_hand
set_class_anim hatongone			mann.hut_auf_weg
set_class_anim hatonhead_wall 		mann.wand_muetze_auf
set_class_anim hatofhead_wall 		mann.wand_muetze_ab

// --------- Feuer löschen ---------

set_class_anim fireaccident_start	mann.flammen_loeschen_start
set_class_anim fireaccident_loop	mann.flammen_loeschen_loop
set_class_anim fireaccident_end		mann.flammen_loeschen_end

// ------------ Werkzeug -----------
set_class_anim tooltakeout_a		mann.werkzeug_raus_a
set_class_anim tooltakeout_b		mann.werkzeug_raus_b
set_class_anim toolputaway_a		mann.werkzeug_weg_a
set_class_anim toolputaway_b		mann.werkzeug_weg_b

set_class_anim tooltakeoutwall_a	mann.wand_hacke_raus_ohne
set_class_anim tooltakeoutwall_b	mann.wand_hacke_raus_mit
set_class_anim toolputawaywall_a	mann.wand_hacke_weg_mit
set_class_anim toolputawaywall_b	mann.wand_hacke_weg_ohne

set_class_anim tooltakeoutleft_a	mann.werkzeug_raus_links_a
set_class_anim tooltakeoutleft_b	mann.werkzeug_raus_links_b
set_class_anim toolputawayleft_a	mann.werkzeug_weg_links_a
set_class_anim toolputawayleft_b	mann.werkzeug_weg_links_b

// ------------- Kiste -------------
set_class_anim takeboxa				mann.kiste_nehmen_ohne
set_class_anim takeboxb				mann.kiste_nehmen_mit
set_class_anim putboxa				mann.kiste_ablegen_mit
set_class_anim putboxb				mann.kiste_ablegen_ohne

// ------------ Schalter ----------
set_class_anim switchnorm			mann.schalten_normal
set_class_anim switchup				mann.schalten_hoch

// ----------- Schatzbuch ---------
set_class_anim read					mann.lesen

// ------------ Aufheben ----------
set_class_anim bend					mann.buecken
set_class_anim benda				mann.buecken_a
set_class_anim bendb				mann.buecken_b

set_class_anim putwall				mann.wand_ablegen
set_class_anim takewall				mann.wand_aufnehmen

set_class_anim scout				mann.spaehen
set_class_anim showright			mann.zeigen_rechts
set_class_anim showleft				mann.zeigen_links
set_class_anim showup				mann.zeigen_oben

// *******************************************************************
// Produktion
// *******************************************************************
// ----------- Aufbau ------------
set_class_anim unten_rechts			mann.aufbauen_rechts_unten
set_class_anim unten_rechtsholz		mann.aufbauen_rechts_u_holz
set_class_anim unten_rechtsmetall	mann.aufbauen_rechts_u_metall
set_class_anim unten_rechtsstein	mann.aufbauen_rechts_u_stein
set_class_anim unten_links			mann.aufbauen_links_unten
set_class_anim unten_linksholz		mann.aufbauen_links_u_holz
set_class_anim unten_linksmetall	mann.aufbauen_links_u_metall
set_class_anim unten_linksstein		mann.aufbauen_links_u_stein
set_class_anim oben_rechts			mann.aufbauen_rechts_oben
set_class_anim oben_rechtsholz		mann.aufbauen_rechts_o_holz
set_class_anim oben_rechtsmetall	mann.aufbauen_rechts_o_metall
set_class_anim oben_rechtsstein		mann.aufbauen_rechts_o_stein
set_class_anim oben_links			mann.aufbauen_links_oben
set_class_anim oben_linksholz		mann.aufbauen_links_o_holz
set_class_anim oben_linksmetall		mann.aufbauen_links_o_metall
set_class_anim oben_linksstein		mann.aufbauen_links_o_stein

// ------------- Graben -------------
set_class_anim digup				mann.buddeln_oben_normal
set_class_anim digupaccident		mann.buddeln_oben_norm_unfall
set_class_anim digfront				mann.buddeln_vorn
set_class_anim digfrontwood			mann.buddeln_vorn_holz
set_class_anim digfrontmetal		mann.buddeln_vorn_metall
set_class_anim digfrontstone		mann.buddeln_vorn_stein

set_class_anim digdown				mann.buddeln_unten
set_class_anim digleft				mann.buddeln_unten
set_class_anim digright				mann.buddeln_unten
set_class_anim digdownaccident		mann.buddeln_unten_unfall

set_class_anim digdownwood			mann.buddeln_unten_holz
set_class_anim digleftwood			mann.buddeln_unten_holz
set_class_anim digrightwood			mann.buddeln_unten_holz
set_class_anim digdownaccidentwood	mann.buddeln_u_holz_unfall

set_class_anim digdownstone			mann.buddeln_unten_stein
set_class_anim digleftstone			mann.buddeln_unten_stein
set_class_anim digrightstone		mann.buddeln_unten_stein
set_class_anim digdownaccidentstone	mann.buddeln_u_stein_unfall

set_class_anim digdownmetal			mann.buddeln_unten_metall
set_class_anim digleftmetal			mann.buddeln_unten_metall
set_class_anim digrightmetal		mann.buddeln_unten_metall
set_class_anim digdownaccidentmetal	mann.buddeln_u_metall_unfall

set_class_anim digclimbup			mann.kletter_hack_oben			;# U
set_class_anim digclimbdown			mann.kletter_hack_unten			;# U
set_class_anim digclimbleft			mann.kletter_hack_links			;# U
set_class_anim digclimbright		mann.kletter_hack_rechts		;# U

// -------- Presslufthammer ---------
set_class_anim airhammdownend		mann.pressluft_unten_end
set_class_anim airhammdownloop		mann.pressluft_unten_loop
set_class_anim airhammdownstart		mann.pressluft_unten_start
set_class_anim airhammdownaccident	mann.pressluft_unten_unfall
set_class_anim airhammd2f			mann.pressluft_unten_zu_vorne

set_class_anim airhammf2u			mann.pressluft_vorne_zu_oben
set_class_anim airhammupend			mann.pressluft_oben_end
set_class_anim airhammuploop		mann.pressluft_oben_loop
set_class_anim airhammupstart		mann.pressluft_oben_start
set_class_anim airhammupaccident	mann.pressluft_oben_unfall

set_class_anim airhammu2f			mann.pressluft_oben_zu_vorne
set_class_anim airhammfrontend		mann.pressluft_vorne_end
set_class_anim airhammfrontloop		mann.pressluft_vorne_loop
set_class_anim airhammfrontstart	mann.pressluft_vorne_start
set_class_anim airhammf2d			mann.pressluft_vorne_zu_unten

set_class_anim airhammstill			mann.pressluft_stand
set_class_anim airhammwallstill		mann.pressluft_wand_stand

set_class_anim airhammstarta		mann.pressluft_raus_ohne
set_class_anim airhammstartb		mann.pressluft_raus_mit
set_class_anim airhammendb			mann.pressluft_weg_ohne
set_class_anim airhammenda			mann.pressluft_weg_mit

set_class_anim airhammwallenda		mann.pressluft_wand_weg_mit
set_class_anim airhammwallendb		mann.pressluft_wand_weg_ohne
set_class_anim airhammwallstartb	mann.pressluft_wand_raus_mit
set_class_anim airhammwallstarta	mann.pressluft_wand_raus_ohne

set_class_anim airhammwallu2l		mann.pressluft_w_oben_zu_links
set_class_anim airhammwalllend		mann.pressluft_wand_links_end
set_class_anim airhammwalllloop		mann.pressluft_wand_links_loop
set_class_anim airhammwalllstart	mann.pressluft_wand_links_start
set_class_anim airhammwalll2u		mann.pressluft_w_links_zu_oben

set_class_anim airhammwalld2r		mann.pressluft_w_unten_zu_rechts
set_class_anim airhammwallrend		mann.pressluft_wand_rechts_end
set_class_anim airhammwallrloop		mann.pressluft_wand_rechts_loop
set_class_anim airhammwallrstart	mann.pressluft_wand_rechts_start
set_class_anim airhammwallr2d		mann.pressluft_w_rechts_zu_unten

set_class_anim airhammwallr2u		mann.pressluft_w_rechts_zu_oben
set_class_anim airhammwalluend		mann.pressluft_wand_oben_end
set_class_anim airhammwalluloop		mann.pressluft_wand_oben_loop
set_class_anim airhammwallustart	mann.pressluft_wand_oben_start
set_class_anim airhammwallu2r		mann.pressluft_w_oben_zu_rechts

set_class_anim airhammwalll2d		mann.pressluft_w_links_zu_oben
set_class_anim airhammwalldend		mann.pressluft_wand_unten_end
set_class_anim airhammwalldloop		mann.pressluft_wand_unten_loop
set_class_anim airhammwalldstart	mann.pressluft_wand_unten_start
set_class_anim airhammwalld2l		mann.pressluft_w_unten_zu_rechts

// ------- Kristallstrahl -------
set_class_anim laserdrillupstart	mann.laserbohrer_oben_start
set_class_anim laserdrilluploop		mann.laserbohrer_oben_loop
set_class_anim laserdrillupstop		mann.laserbohrer_oben_end
set_class_anim laserdrillup2fr		mann.laserbohrer_oben_zu_vorne

set_class_anim laserdrillfrstart	mann.laserbohrer_vorne_start
set_class_anim laserdrillfrloop		mann.laserbohrer_vorne_loop
set_class_anim laserdrillfrstop		mann.laserbohrer_vorne_end
set_class_anim laserdrillfr2up		mann.laserbohrer_vorne_zu_oben

// ------------ Pilz ------------
set_class_anim hackstart			mann.hacken_start
set_class_anim hackloop				mann.hacken_loop
set_class_anim hackend				mann.hacken_end
set_class_anim hackaccidenta		mann.hacken_unfall_a

set_class_anim hackfitstart			mann.hacken_fit_start
set_class_anim hackfitloop			mann.hacken_fit_loop
set_class_anim hackfitend			mann.hacken_fit_end

set_class_anim hacktiredstart		mann.hacken_muede_start
set_class_anim hacktiredloop		mann.hacken_muede_loop
set_class_anim hacktiredend			mann.hacken_muede_end
// Motorsaege
set_class_anim sawhorend			mann.saege_horizontal_end
set_class_anim sawhorloop			mann.saege_horizontal_loop
set_class_anim sawhorstart			mann.saege_horizontal_start
set_class_anim sawvertikalend		mann.saege_vertikal_end
set_class_anim sawvertikalloop		mann.saege_vertikal_loop
set_class_anim sawvertikalstart		mann.saege_vertikal_start

// --- allgemein beim Arbeiten ---
// werkeln
set_class_anim work					mann.werkeln
set_class_anim workholz				mann.werkeln_holz
set_class_anim workmetall			mann.werkeln_metall
set_class_anim workstein			mann.werkeln_stein
// werkeln boden
set_class_anim workatfloor			mann.werkeln_boden
set_class_anim workfloorholz		mann.werkeln_boden_holz
set_class_anim workfloormetall		mann.werkeln_boden_metall
set_class_anim workfloorstein		mann.werkeln_boden_stein
// werkeln feuer
set_class_anim workatfire			mann.werkeln_feuer
set_class_anim worktopholz			mann.werkeln_oben_holz
set_class_anim worktopmetall		mann.werkeln_oben_metall
set_class_anim worktopstein			mann.werkeln_oben_stein
// haemmern
set_class_anim hammerend			mann.haemmern_end
set_class_anim hammerloop			mann.haemmern_loop
set_class_anim hammerloopholz		mann.haemmern_loop_holz
set_class_anim hammerloopmetall		mann.haemmern_loop_metall
set_class_anim hammerloopstein		mann.haemmern_loop_stein
set_class_anim hammerstart			mann.haemmern_start
set_class_anim hammeraccidenta		mann.haemmern_unfall_a
// schraeg hacken
set_class_anim hackqustart			mann.hacken_quer_start
set_class_anim hackquloop			mann.hacken_quer_loop
set_class_anim hackquend			mann.hacken_quer_end
// hobeln
set_class_anim planestart			mann.hobeln_start
set_class_anim planeloop			mann.hobeln_loop
set_class_anim planeend				mann.hobeln_end
set_class_anim planeaccident		mann.hobeln_unfall
// saegen
set_class_anim foxtailstart			mann.fuchsschwanz_start
set_class_anim foxtailloop			mann.fuchsschwanz_loop
set_class_anim foxtailstop			mann.fuchsschwanz_end
// akkuschrauber
set_class_anim scewstart			mann.akkuschrauber_start
set_class_anim scewloop				mann.akkuschrauber_loop
set_class_anim scewstop				mann.akkuschrauber_end
// steinmeisseln
set_class_anim carvestonestart		mann.meisseln_stehend_start
set_class_anim carvestoneloop		mann.meisseln_stehend_loop
set_class_anim carvestonestop		mann.meisseln_stehend_end
// schweissen
set_class_anim weldglason			mann.schweissbrille_auf
set_class_anim weldglasoff			mann.schweissbrille_ab
set_class_anim weldstart			mann.schweissen_start
set_class_anim weldloop				mann.schweissen_loop
set_class_anim weldstop				mann.schweissen_end
// waffenfabrik
set_class_anim weaponfactoryup		mann.waffenfabrik_a
set_class_anim weaponfactorydown	mann.waffenfabrik_b
// kurbeln
set_class_anim windend				mann.kurbeln_end
set_class_anim windloop				mann.kurbeln_loop
set_class_anim windstart			mann.kurbeln_start
// erfinden
set_class_anim invent_a				mann.erfinden_a
set_class_anim invent_b				mann.erfinden_b
set_class_anim invent_c				mann.erfinden_c
set_class_anim invent_done			mann.erfinden_geschafft
// andere
set_class_anim pressbutton			mann.knopf_druecken
set_class_anim kickmachine			mann.treten_maschine
set_class_anim kontrol				mann.kontrolle
set_class_anim fire					mann.pusten_feuer
// auf Tisch ablegen
set_class_anim put					mann.ablegen
set_class_anim puta					mann.ablegen_a
set_class_anim putb					mann.ablegen_b

// ----------- Farm --------------
set_class_anim farmsow				mann.farm_streuen
// --------- Brauerei ------------
set_class_anim stir					mann.ruehren
set_class_anim stirkettle			mann.ruehren_kessel
set_class_anim stirstart			mann.ruehren_start
set_class_anim stirloop				mann.ruehren_loop
set_class_anim stirstop				mann.ruehren_end
// ------------ Bar --------------
set_class_anim barkeeper			mann.barkeeper
set_class_anim traystand			mann.tablett_stand
set_class_anim trayservea			mann.tablett_servieren_a
set_class_anim trayserveb			mann.tablett_servieren_b
set_class_anim trayservec			mann.tablett_servieren_c
set_class_anim trayputawaya			mann.tablett_weg_mit
set_class_anim trayputawayb			mann.tablett_weg_ohne
// ----------- Lager -------------
set_class_anim putjump				mann.lager_ablegen
set_class_anim putjumphigh			mann.lager_ablegen_mitte
set_class_anim putjumphighest		mann.lager_ablegen_hoch
// ---------- Wachhaus -----------
set_class_anim guardwalk			mann.patroulieren
// ----------- Dojo --------------
set_class_anim matrixdojo			mann.matrix
set_class_anim standstilldojo		mann.kungfu_standanim
set_class_anim rotateleftdojo		mann.dojo_drehen_kungfu
set_class_anim turn180leftdojo		mann.dojo_drehen_kungfu
// ---------- Theater ------------
set_class_anim stagestart			mann.buehne_start
set_class_anim stagestop			mann.buehne_end
set_class_anim stage_a				mann.buehne_a
set_class_anim stage_b				mann.buehne_b
// ---------- Schule -------------
set_class_anim boardwrite			mann.tafel_schreiben
set_class_anim standsleepstart		mann.stehend_schlafen_start
set_class_anim standsleeploop		mann.stehend_schlafen_loop
set_class_anim standsleepstop		mann.stehend_schlafen_end
// ---------- Tempel -------------
set_class_anim praystart			mann.beten_start
set_class_anim prayloop				mann.beten_loop
set_class_anim praystop				mann.beten_end
set_class_anim tempelwalkup			mann.tempel_a
set_class_anim tempelwalkdown		mann.tempel_b
// ----------- Disco -------------
set_class_anim dja					mann.dj_a
set_class_anim djc					mann.dj_c
set_class_anim djhigh				mann.dj_high {eyes c l l l l l o o o o r r r r r c }
// --------- Krankenhaus ---------
set_class_anim heala				mann.heilen_a
set_class_anim healb				mann.heilen_b
set_class_anim healshot				mann.heilen_spritze
set_class_anim healelektro			mann.heilen_elektro
// ------- Mittelkueche ----------
set_class_anim cookmiddleastart		mann.mittelkueche_a_start
set_class_anim cookmiddlealoop		mann.mittelkueche_a_loop
set_class_anim cookmiddleastop		mann.mittelkueche_a_end
set_class_anim cookmiddlebstart		mann.mittelkueche_b_start
set_class_anim cookmiddlebstop		mann.mittelkueche_b_end
// ------- Industkueche ----------
set_class_anim cookindustastart		mann.indust_kueche_a_start
set_class_anim cookindustaloop		mann.indust_kueche_a_loop
set_class_anim cookindustastop		mann.indust_kueche_a_end
set_class_anim cookindustbstart		mann.indust_kueche_b_start
set_class_anim cookindustbloop		mann.indust_kueche_b_loop
set_class_anim cookindustbstop		mann.indust_kueche_b_end
// ---------- Hochofen -----------
set_class_anim pullrope				mann.strippe_ziehen
// ----------- Labor -------------
set_class_anim calculator			mann.taschenrechner

// *******************************************************************
// Freizeit
// *******************************************************************

set_class_anim sitdown				mann.hinsetzen
set_class_anim sitfloorstill		mann.sitzen_boden_stand
set_class_anim sitfloordrink		mann.sitzen_boden_trinken
set_class_anim standup				mann.aufstehen

set_class_anim superman				mann.superman
set_class_anim drinkpotion			mann.heiltrank

set_class_anim sitdown_chair		mann.hinsetzen_stuhl
set_class_anim sitchairloop			mann.sitzen_stuhl_loop
set_class_anim sitchairbore			mann.sitzen_stuhl_langw
set_class_anim standup_chair		mann.aufstehen_stuhl

set_class_anim couchstart			mann.hinsetzen_sofa
set_class_anim couchloopa			mann.sitzen_sofa_loop
set_class_anim couchloopb			mann.sitzen_sofa_loop_b
set_class_anim couchloopc			mann.sitzen_sofa_loop_c
set_class_anim couchloopd			mann.sitzen_sofa_loop_d
set_class_anim couchstop			mann.aufstehen_sofa
// ------------ essen ------------
set_class_anim sitflooreat			mann.sitzen_boden_essen
set_class_anim sitchaireat			mann.sitzen_stuhl_essen
set_class_anim sitfloorsoup			mann.sitzen_boden_suppe
set_class_anim sitchairsoup			mann.sitzen_stuhl_suppe
set_class_anim sitfloorshake		mann.sitzen_boden_trinken
set_class_anim sitchairshake		mann.sitzen_stuhl_trinken
// ---------- schlafen -----------
set_class_anim laydown				mann.stand_zu_schlafen
set_class_anim sleepside			mann.schlafen_boden_loop
set_class_anim sleeptosit			mann.schlafen_zu_sitz
set_class_anim sittosleep			mann.sitz_zu_schlafen
set_class_anim sleepwalk			mann.schlafwandeln
set_class_anim sleeptostand			mann.schlafen_zu_stand
// ------------ baden ------------
set_class_anim sitdown_bath			mann.hinsetzen_wanne
set_class_anim bathstart			mann.baden_start
set_class_anim bathloop				mann.baden_loop
set_class_anim bathstop				mann.baden_end
set_class_anim standup_bath			mann.aufstehen_wanne
set_class_anim washface				mann.gesicht_reiben
// ------------- Sex -------------
set_class_anim sexfloorastart		mann.sex_boden_a_start
set_class_anim sexflooraloop		mann.sex_boden_a_loop
set_class_anim sexflooraend			mann.sex_boden_a_end

set_class_anim sexfloorbstart		mann.sex_boden_b_start
set_class_anim sexfloorbloop		mann.sex_boden_b_loop
set_class_anim sexfloorbend			mann.sex_boden_b_end
// ------------- Bar -------------
set_class_anim sitchairbeerstand	mann.trinken_stand
set_class_anim sitchairbeerstart	mann.trinken_start
set_class_anim sitchairbeerloop		mann.trinken_stand_loop
set_class_anim sitchairbeerdrink	mann.trinken_loop
set_class_anim sitchairbeertalka	mann.trinken_dialog_a
set_class_anim sitchairbeertalkb	mann.trinken_dialog_b
set_class_anim sitchairbeertalkc	mann.trinken_dialog_c
set_class_anim sitchairbeerstop		mann.trinken_end
set_class_anim sitchairorder		mann.sitzen_stuhl_bestellen
set_class_anim tapdrinkstart		mann.zapfhahn_trinken_start
set_class_anim tapdrinkloop			mann.zapfhahn_trinken_loop
set_class_anim tapdrinkstop			mann.zapfhahn_trinken_end
set_class_anim drinkatbar			mann.trinken_bar
// -------- Fitnessstudio --------
set_class_anim punch				mann.boxen
set_class_anim puncha				mann.schlagen_a
set_class_anim punchc				mann.schlagen_c
set_class_anim punchside			mann.schlagen_zur_seite
// Streckübungen und Händeknacksen
set_class_anim warmupcstart			mann.training_c_start
set_class_anim warmupcloop			mann.training_c_loop
set_class_anim warmupcstop			mann.training_c_end
set_class_anim warmupe				mann.training_e
// Treten
set_class_anim kicka				mann.treten_a
set_class_anim kickb				mann.treten_b
set_class_anim kickc				mann.treten_c
set_class_anim kickd				mann.treten_d
// Hanteltraining
set_class_anim handlestemstarta		mann.fitness_hantel_start_a
set_class_anim handlestemstartb		mann.fitness_hantel_start_b
set_class_anim handlestemloop		mann.fitness_hantel_loop
set_class_anim handlestemstopa		mann.fitness_hantel_end_a
set_class_anim handlestemstopb		mann.fitness_hantel_end_b
// Punchingball
set_class_anim punchingballa		mann.fitness_punchball_a
set_class_anim punchingballacc		mann.fitness_punchball_unfall
// Standjoggen
set_class_anim standjogstart		mann.stehend_joggen_start
set_class_anim standjogloop			mann.stehend_joggen_loop
set_class_anim standjogstop			mann.stehend_joggen_end
// Klimmzuege
set_class_anim pullupstart			mann.klimmzuege_start
set_class_anim pulluploop			mann.klimmzuege_loop
set_class_anim pullupstop			mann.klimmzuege_end
// ---------- Theater ------------
set_class_anim boo					mann.ausbuhen
set_class_anim cheer				mann.jubeln
set_class_anim applaud				mann.applaudieren
// -------- Bowlingbahn ----------
set_class_anim bowla				mann.bowlen_a
set_class_anim bowlb				mann.bowlen_b
set_class_anim bowlc				mann.bowlen_c
set_class_anim bowlwin				mann.bowl_gewinnen
set_class_anim bowllose				mann.bowl_verlieren
// ----------- Disco -------------
set_class_anim discoa				mann.disco_a
set_class_anim discoc				mann.disco_c
set_class_anim discod				mann.disco_d
// -------- Krankenhaus ----------
set_class_anim illstart				mann.krank_start
set_class_anim illloop				mann.krank_loop
set_class_anim illstop				mann.krank_end
set_class_anim illnormal			mann.krank_leicht
// -------- Mittel_wohn ----------
set_class_anim rockchairstart		mann.schaukelstuhl_start
set_class_anim rockchairloop		mann.schaukelstuhl_loop
set_class_anim rockchairstop		mann.schaukelstuhl_end
set_class_anim spinstart			mann.spinnrad_start
set_class_anim spinloop				mann.spinnrad_loop
set_class_anim spinstop				mann.spinnrad_end
// ------------ Bad --------------
set_class_anim showera				mann.duschen_a
set_class_anim showerb				mann.duschen_b {eyes c c 9 9 9 9 9 9 9 9 9 9 9 9 9 c}
// ---------- Bordell ------------
set_class_anim brothela				mann.bordell_a
set_class_anim brothelb				mann.bordell_b
set_class_anim brothelcstart		mann.bordell_c_start
set_class_anim brothelcloop			mann.bordell_c_loop
set_class_anim brothelcstop			mann.bordell_c_end
// ----------- Bett --------------
set_class_anim bedreadastart		mann.bett_a_lesen_start
set_class_anim bedreadaloop			mann.bett_a_lesen_loop
set_class_anim bedreadastop			mann.bett_a_lesen_end
set_class_anim bedreadbstart		mann.bett_b_lesen_start
set_class_anim bedreadbloop			mann.bett_b_lesen_loop
set_class_anim bedreadbstop			mann.bett_b_lesen_end
set_class_anim bedsleepstart		mann.schlafen_bett_start
set_class_anim bedsleepstart		mann.schlafen_bett_loop
set_class_anim bedsleepstart		mann.schlafen_bett_end
set_class_anim bedastart			mann.schlafen_bett_a_start
set_class_anim bedaloop				mann.schlafen_bett_a_loop
set_class_anim bedastop				mann.schlafen_bett_a_end
set_class_anim bedbstart			mann.schlafen_bett_b_start
set_class_anim bedbloop				mann.schlafen_bett_b_loop
set_class_anim bedbstop				mann.schlafen_bett_b_end
// --------- Mittelbad -----------
set_class_anim handwashmiddle		mann.mittelbad_haende
set_class_anim toiletmiddlestart	mann.mittelbad_klo_start
set_class_anim toiletmiddleloop		mann.mittelbad_klo_loop
set_class_anim toiletmiddlestop		mann.mittelbad_klo_end
// --------- Industbad -----------
set_class_anim handwashindust		mann.indust_bad_haende
set_class_anim toiletinduststart	mann.indust_bad_klo_start
set_class_anim toiletindustloop		mann.indust_bad_klo_loop
set_class_anim toiletinduststop		mann.indust_bad_klo_end
// --------- Industwohn ----------
set_class_anim campinduststart		mann.indust_wohn_liege_start
set_class_anim campindustloop		mann.indust_wohn_liege_loop
set_class_anim campinduststop		mann.indust_wohn_liege_end
// --------- Goldenbad -----------
set_class_anim bathgoldenastart		mann.gold_baden_start_a
set_class_anim bathgoldenbstart		mann.gold_baden_start_b
set_class_anim bathgoldenbloop		mann.gold_baden_loop
set_class_anim bathgoldenbstop		mann.gold_baden_end
// --------- Goldenwohn ----------
set_class_anim tvgoldenstart		mann.fernsehen_start
set_class_anim tvgoldenloop			mann.fernsehen_loop
set_class_anim tvgoldenstop			mann.fernsehen_end
set_class_anim pianogoldenstart		mann.klavier_start
set_class_anim pianogoldenloop		mann.klavier_loop
set_class_anim pianogoldenstop		mann.klavier_end

// *******************************************************************
// Fueller-Animationen
// *******************************************************************

// --------- allgemeine ----------
// Pfeife rauchen
set_class_anim smokepipestart		mann.pfeife_rauchen_start
set_class_anim smokepipeloop		mann.pfeife_rauchen_loop
set_class_anim smokepipestop		mann.pfeife_rauchen_end
// auf Rand setzen
set_class_anim sitdown_edge			mann.hinsetzen_rand
set_class_anim sitedgeloopa			mann.sitzen_rand_loop_a
set_class_anim sitedgeloopb			mann.sitzen_rand_loop_b
set_class_anim sitedgestill			mann.sitzen_rand_stand
set_class_anim standup_edge			mann.aufstehen_rand
set_class_anim layedgestart			mann.randliegen_start
set_class_anim layedgestand			mann.randliegen_stand
set_class_anim layedgestill			mann.randliegen_standanim
set_class_anim layedgestop			mann.randliegen_end
// Schmetterling fangen
set_class_anim butterflya			mann.schmetterling_oben
set_class_anim butterflyb			mann.schmetterling_unten
set_class_anim butterflyc			mann.schmetterling_vorn
// Handstand
set_class_anim handstandstart		mann.handstand_start
set_class_anim handstandloop		mann.handstand_loop
set_class_anim handstandstop		mann.handstand_end
// Seilspringen
set_class_anim jumproping			mann.seilspringen
// Liegestütze
set_class_anim pressupstart			mann.liegestuetze_start
set_class_anim pressuploop			mann.liegestuetze_loop
set_class_anim pressupstop			mann.liegestuetze_end
// stricken
set_class_anim knitstart			mann.stricken_start
set_class_anim knitloop				mann.stricken_loop
set_class_anim knitstop				mann.stricken_end
// schnitzen
set_class_anim carvestart			mann.schnitzen_start
set_class_anim carveloop			mann.schnitzen_loop {face 11 15 15 11 c c}
set_class_anim carvestop			mann.schnitzen_end
// Hackisack
set_class_anim footbagstart			mann.sacki_start
set_class_anim footbagloop			mann.sacki_loop
set_class_anim footbagstop			mann.sacki_end
set_class_anim footbaga				mann.sacki_a
set_class_anim footbagb				mann.sacki_b
set_class_anim footbagc				mann.sacki_c
set_class_anim footbagd				mann.sacki_d
// zuhören
set_class_anim listenastart			mann.zuhoeren_a_start
set_class_anim listenaloop			mann.zuhoeren_a_loop
set_class_anim listenastop			mann.zuhoeren_a_end
set_class_anim listenbstart			mann.zuhoeren_b_start
set_class_anim listenbloop			mann.zuhoeren_b_loop
set_class_anim listenbstop			mann.zuhoeren_b_end
// sonstige
set_class_anim scratch				mann.kratzen
set_class_anim scratchhead			mann.kopf_kratzen
set_class_anim breathe				mann.aufatmen
set_class_anim stretch				mann.recken
set_class_anim hungry				mann.hungrig
set_class_anim jumpa				mann.hopsen_a
set_class_anim jumpb				mann.hopsen_b
set_class_anim cartwheel			mann.radschlagen
set_class_anim leftright			mann.blicken_rechts_links
set_class_anim cough				mann.raeuspern
set_class_anim teeter_w				mann.wippen
set_class_anim teeter_t				mann.verlegen
set_class_anim kneebend				mann.kniebeuge
set_class_anim impatient			mann.ungeduldig
set_class_anim wipenose				mann.naseabstreifen
set_class_anim tired				mann.erschoepft
set_class_anim wait					mann.warten
set_class_anim dimensiongate_in		mann.dimensionstor_start
set_class_anim dimensiongate_out	mann.dimensionstor_end
// ----------- an PS -----------
// lehnen
set_class_anim leanstart			mann.anlehnen_start
set_class_anim leanloop				mann.anlehnen_loop
set_class_anim leanstop				mann.anlehnen_end
// am Feuer
set_class_anim warmhands			mann.haende_waermen
set_class_anim warmbutt				mann.popo_waermen
// Boden wischen
set_class_anim cleanfloorstart		mann.wischen_boden_start
set_class_anim cleanfloorloop		mann.wischen_boden_loop
set_class_anim cleanfloorstop		mann.wischen_boden_end
// trinken
set_class_anim drinktubstart		mann.bottich_trinken_start
set_class_anim drinktubloop			mann.bottich_trinken_loop
set_class_anim drinktubstop			mann.bottich_trinken_end
// Zaunsitzen
set_class_anim sitfencestart		mann.sitzen_zaun_start
set_class_anim sitfenceloop			mann.sitzen_zaun_loop
set_class_anim sitfencestop			mann.sitzen_zaun_end
// sonstige
set_class_anim vaultover			mann.bockspringen
set_class_anim washhands			mann.haende_waschen
set_class_anim taichi				mann.taichi
set_class_anim takedrugs			mann.drogen_nehmen
set_class_anim sniffatfood			mann.schnuppern_essen
set_class_anim kickdoor				mann.tuer_auftreten
set_class_anim electrify			mann.elektroschock
set_class_anim jumpfire				mann.feuer_springen
set_class_anim gymanvil				mann.turnen_amboss

// *******************************************************************
// Rede-Animationen
// *******************************************************************

// ------- Begruessung ---------
set_class_anim talkgrnt				mann.dialog_gruss_neutral
set_class_anim talkgrng				mann.dialog_gruss_negativ
set_class_anim talkgrpo				mann.dialog_gruss_positiv

// --------- Redner -------------
// neutral
set_class_anim talkacnta			mann.dialog_ac_neutral_a
set_class_anim talkacntaq			mann.dialog_ac_neutral_a_frage
set_class_anim talkacntap			mann.dialog_ac_neutral_a_punkt
set_class_anim talkacntae			mann.dialog_ac_neutral_a_ruf
set_class_anim talkacntb			mann.dialog_ac_neutral_b
set_class_anim talkacntbq			mann.dialog_ac_neutral_b_frage
set_class_anim talkacntbp			mann.dialog_ac_neutral_b_punkt
set_class_anim talkacntbe			mann.dialog_ac_neutral_b_ruf
set_class_anim talkacntc			mann.dialog_ac_neutral_c
set_class_anim talkacntcq			mann.dialog_ac_neutral_c_frage
set_class_anim talkacntcp			mann.dialog_ac_neutral_c_punkt
set_class_anim talkacntce			mann.dialog_ac_neutral_c_ruf
// positiv
set_class_anim talkacpoa			mann.dialog_ac_positiv_a
set_class_anim talkacpoaq			mann.dialog_ac_positiv_a_frage
set_class_anim talkacpoap			mann.dialog_ac_positiv_a_punkt
set_class_anim talkacpoae			mann.dialog_ac_positiv_a_ruf
set_class_anim talkacpob			mann.dialog_ac_positiv_b
set_class_anim talkacpobq			mann.dialog_ac_positiv_b_frage
set_class_anim talkacpobp			mann.dialog_ac_positiv_b_punkt
set_class_anim talkacpobe			mann.dialog_ac_positiv_b_ruf
set_class_anim talkacpoc			mann.dialog_ac_positiv_c
set_class_anim talkacpocq			mann.dialog_ac_positiv_c_frage
set_class_anim talkacpocp			mann.dialog_ac_positiv_c_punkt
set_class_anim talkacpoce			mann.dialog_ac_positiv_c_ruf
// negativ
set_class_anim talkacnga			mann.dialog_ac_negativ_a
set_class_anim talkacngaq			mann.dialog_ac_negativ_a_frage
set_class_anim talkacngap			mann.dialog_ac_negativ_a_punkt
set_class_anim talkacngae			mann.dialog_ac_negativ_a_ruf
set_class_anim talkacngb			mann.dialog_ac_negativ_b
set_class_anim talkacngbq			mann.dialog_ac_negativ_b_frage
set_class_anim talkacngbp			mann.dialog_ac_negativ_b_punkt
set_class_anim talkacngbe			mann.dialog_ac_negativ_b_ruf
set_class_anim talkacngc			mann.dialog_ac_negativ_c
set_class_anim talkacngcq			mann.dialog_ac_negativ_c_frage
set_class_anim talkacngcp			mann.dialog_ac_negativ_c_punkt
set_class_anim talkacngce			mann.dialog_ac_negativ_c_ruf

// ---- reagierender Zuhörer ----
// neutral
set_class_anim talkrenta			mann.dialog_re_neutral_a
set_class_anim talkrentaq			mann.dialog_re_neutral_a_frage
set_class_anim talkrentap			mann.dialog_re_neutral_a_punkt
set_class_anim talkrentae			mann.dialog_re_neutral_a_ruf
set_class_anim talkrentb			mann.dialog_re_neutral_b
set_class_anim talkrentbq			mann.dialog_re_neutral_b_frage
set_class_anim talkrentbp			mann.dialog_re_neutral_b_punkt
set_class_anim talkrentbe			mann.dialog_re_neutral_b_ruf
set_class_anim talkrentc			mann.dialog_re_neutral_c
set_class_anim talkrentcq			mann.dialog_re_neutral_c_frage
set_class_anim talkrentcp			mann.dialog_re_neutral_c_punkt
set_class_anim talkrentce			mann.dialog_re_neutral_c_ruf
// positiv
set_class_anim talkrepoa			mann.dialog_re_positiv_a
set_class_anim talkrepoaq			mann.dialog_re_positiv_a_frage
set_class_anim talkrepoap			mann.dialog_re_positiv_a_punkt
set_class_anim talkrepoae			mann.dialog_re_positiv_a_ruf
set_class_anim talkrepob			mann.dialog_re_positiv_b
set_class_anim talkrepobq			mann.dialog_re_positiv_b_frage
set_class_anim talkrepobp			mann.dialog_re_positiv_b_punkt
set_class_anim talkrepobe			mann.dialog_re_positiv_b_ruf
set_class_anim talkrepoc			mann.dialog_re_positiv_c
set_class_anim talkrepocq			mann.dialog_re_positiv_c_frage
set_class_anim talkrepocp			mann.dialog_re_positiv_c_punkt
set_class_anim talkrepoce			mann.dialog_re_positiv_c_ruf
// negativ
set_class_anim talkrenga			mann.dialog_re_negativ_a
set_class_anim talkrengaq			mann.dialog_re_negativ_a_frage
set_class_anim talkrengap			mann.dialog_re_negativ_a_punkt
set_class_anim talkrengae			mann.dialog_re_negativ_a_ruf
set_class_anim talkrengb			mann.dialog_re_negativ_b
set_class_anim talkrengbq			mann.dialog_re_negativ_b_frage
set_class_anim talkrengbp			mann.dialog_re_negativ_b_punkt
set_class_anim talkrengbe			mann.dialog_re_negativ_b_ruf
set_class_anim talkrengc			mann.dialog_re_negativ_c
set_class_anim talkrengcq			mann.dialog_re_negativ_c_frage
set_class_anim talkrengcp			mann.dialog_re_negativ_c_punkt
set_class_anim talkrengce			mann.dialog_re_negativ_c_ruf

// ------ passiver Zuhörer ------
// neutral
set_class_anim talkpanta			mann.dialog_pa_neutral_a
set_class_anim talkpantb			mann.dialog_pa_neutral_b
set_class_anim talkpantc			mann.dialog_pa_neutral_c
// positiv
set_class_anim talkpapoa			mann.dialog_pa_positiv_a
set_class_anim talkpapob			mann.dialog_pa_positiv_b
set_class_anim talkpapoc			mann.dialog_pa_positiv_c
// negativ
set_class_anim talkpanga			mann.dialog_pa_negativ_a
set_class_anim talkpangb			mann.dialog_pa_negativ_b
set_class_anim talkpangc			mann.dialog_pa_negativ_c

// --------- troesten -----------
set_class_anim tocomfort			mann.schulter_klopfen
set_class_anim getcomfort			mann.schulter_geklopft_werden

// Aus Kompatibilitätsgründen:
set_class_anim talka				mann.dialog_ac_neutral_a
set_class_anim talkd				mann.dialog_ac_neutral_b
set_class_anim talko				mann.dialog_ac_neutral_c
set_class_anim talkp				mann.dialog_ac_positiv_a
set_class_anim talkq				mann.dialog_ac_positiv_b
set_class_anim talkr				mann.dialog_ac_positiv_c
set_class_anim talkk				mann.dialog_ac_negativ_b
set_class_anim talkc				mann.dialog_pa_neutral_c
set_class_anim talkn				mann.dialog_re_neutral_b
set_class_anim talke				mann.dialog_re_neutral_c
set_class_anim talkf				mann.dialog_re_positiv_a
set_class_anim talkg				mann.dialog_re_positiv_c
set_class_anim talki				mann.dialog_re_negativ_a
set_class_anim talkb				mann.dialog_re_negativ_b
set_class_anim talks				mann.dialog_re_negativ_c
set_class_anim talkh				mann.dialog_gruss_neutral
set_class_anim talkm				mann.dialog_gruss_positiv
// Ende Kompatibilität

// *******************************************************************
// Speziell fuer Kampagne und Trailer
// *******************************************************************

set_class_anim kingbedloopa			mann.koenig_bett_loop_a
set_class_anim kingbedloopb			mann.koenig_bett_loop_b
set_class_anim kingwakeupstop		mann.koenig_bett_wecken_end
set_class_anim kingwakeuploop		mann.koenig_bett_wecken_loop
set_class_anim kingwakeupstart		mann.koenig_bett_wecken_start
set_class_anim kingbadmood			mann.koenig_boese
set_class_anim kingendloop			mann.koenig_ende_loop
set_class_anim kingendtalk			mann.koenig_ende_reden
set_class_anim kingendstart			mann.koenig_ende_start
set_class_anim kingtired			mann.koenig_gaehnen
set_class_anim kinglookupa			mann.koenig_hochgucken_a
set_class_anim kinglookupbloop		mann.koenig_hochgucken_b_loop
set_class_anim kinglookupbstop		mann.koenig_hochgucken_b_end
set_class_anim kingwithoutclock		mann.koenig_ohneuhr_b
set_class_anim kingtalka			mann.koenig_reden_a
set_class_anim kingtalkb			mann.koenig_reden_b
set_class_anim kingsleeploop		mann.koenig_schlafen_loop
set_class_anim kingsitstandanim		mann.koenig_sitzen_standanim
set_class_anim kingturnaround		mann.koenig_umdrehen

set_class_anim swordupstart			mann.schwert_hoch_start
set_class_anim sworduploop			mann.schwert_hoch_loop
set_class_anim swordupend			mann.schwert_hoch_end
set_class_anim kiss					mann.kuessen
set_class_anim shock				mann.schreck
set_class_anim lookup				mann.hoch_gucken
set_class_anim invent_brains		mann.erfinden_brains

set_class_anim insane				mann.spinntder
set_class_anim hermit				mann.einsiedler_a

set_class_anim protecteyesstart		mann.augen_schuetzen_start
set_class_anim protecteyesloop		mann.augen_schuetzen_loop
set_class_anim protecteyesstop		mann.augen_schuetzen_end

set_class_anim rollbarrelstart		mann.rollen_fass_start
set_class_anim rollbarrelloop		mann.rollen_fass_loop
set_class_anim rollbarrelstop		mann.rollen_fass_end

set_class_anim drinkbarrelstart		mann.trinken_fass_start
set_class_anim drinkbarrelloop		mann.trinken_fass_loop
set_class_anim drinkbarrelstop		mann.trinken_fass_end

set_class_anim lookdownstart		mann.schacht_gucken_start
set_class_anim lookdownloop			mann.schacht_gucken_loop
set_class_anim lookdownstop			mann.schacht_gucken_end

set_class_anim rebound				mann.abprallen
set_class_anim dontknow				mann.weissnich {eyes c c c c c o o o o o c c c c c}
set_class_anim stummble				mann.stolpern {eyes c c c 2 2 2 2 1 1 c c}

set_class_anim smokepot				mann.kiffen {eyes c c l l l l l l l l c c r r r r r c c c c 1 1 1 1 9 9 9 9 9 c}
set_class_anim fanfars				mann.fanfare
set_class_anim smokepotstop			mann.joint_austreten
set_class_anim offerjoint			mann.joint_anbieten
set_class_anim offerjointa			mann.joint_anbieten_mit
set_class_anim offerjointb			mann.joint_anbieten_ohne
set_class_anim takejoint			mann.joint_annehmen
set_class_anim takejointa			mann.joint_annehmen_ohne
set_class_anim takejointb			mann.joint_annehmen_mit

// *******************************************************************
// nicht zu geordnet !!!
// *******************************************************************

set_class_anim climbhita			mann.kletter_schlagen_a
set_class_anim climbkicka			mann.kletter_treten_a

set_class_anim stopmove_a			mann.stand_anim_a
set_class_anim stopmove_b			mann.stand_anim_b

set_class_anim skippend				mann.skipp_end
set_class_anim skipploop			mann.skipp_loop
set_class_anim skippstart			mann.skipp_start

set_class_anim throw				mann.schmeissen

// *******************************************************************
// Sonstige Animationen
// *******************************************************************

set_class_anim die					mann.sterben
set_class_anim diewall				mann.kletter_sterben

set_class_anim capflight			mann.muetzenflug

set_class_anim medusa_dead			mann.medusa_tot
set_class_anim medusa_survive		mann.medusa_weiter

set_class_anim shootbow				mann.schiessen_bogen
set_class_anim shootkatschi			mann.schiessen_katschi

set_class_anim gettrapped			mann.plattmachfalle_tot
set_class_anim trappedtostand		mann.plattmachfalle_aufstehen

set_class_anim forgegleipnir		mann.schmieden

set_class_anim magicstart			mann.sesam_start
set_class_anim magicloop			mann.sesam_loop
set_class_anim magicstop			mann.sesam_end

// *******************************************************************
// Kampf-Animationen
// *******************************************************************

set_class_anim standtokungfu	mann.stand_zu_kungfustand
set_class_anim kungfustill		mann.kungfustand
set_class_anim kungfustillani	mann.kungfu_standanim
set_class_anim kungfutostand	mann.kungfustand_zu_stand
set_class_anim kungfuskip		mann.kungfu_taenzel_a

set_class_anim standbackhitl	mann.stand_hinten_get_leicht
set_class_anim standbackhitm	mann.stand_hinten_get_mittel
set_class_anim standbackhith	mann.stand_hinten_get_schwer
set_class_anim standbackhitd	mann.stand_hinten_get_tot

set_class_anim standfronthitl	mann.stand_vorne_get_leicht
set_class_anim standfronthitm	mann.stand_vorne_get_mittel
set_class_anim standfronthith	mann.stand_vorne_get_schwer
set_class_anim standfronthitd	mann.stand_vorne_get_tot

set_class_anim standshieldblo	mann.stand_schild_blo

set_class_anim standtosword	mann.stand_zu_schwertstand
set_class_anim swordstill	mann.schwertstand
set_class_anim swordtostand	mann.schwertstand_zu_stand
set_class_anim swordstillani	mann.schwert_standanim

set_class_anim swordturn	mann.schwert_drehen
set_class_anim swordtwist	mann.schwert_fuchtel
set_class_anim swordsalut	mann.schwert_salut

set_class_anim standtotwohand	mann.stand_zu_zweihand
set_class_anim twohandstill	mann.zweihandstand
set_class_anim twohandtostand	mann.zweihand_zu_stand
set_class_anim twohandstillani	mann.zweihand_standanim

set_class_anim kungfuduck	mann.kungfu_ausw_duck
set_class_anim kungfujump	mann.kungfu_ausw_jump
set_class_anim kungfuside	mann.kungfu_ausw_seite
set_class_anim kungfuback	mann.kungfu_ausw_zurueck

set_class_anim kungfusideb	mann.kungfu_ausw_seite_b
set_class_anim kungfuhitheavyb	mann.kungfu_ausw_schwer_b
set_class_anim kungfuhitdeadb	mann.kungfu_ausw_tot_b

set_class_anim kungfufistmiddle	mann.kungfu_faust_mitte
set_class_anim kungfufisthead	mann.kungfu_faust_oben

set_class_anim kungfumasterhead	mann.kungfu_meisterkick_oben

set_class_anim kungfufootmiddle	mann.kungfu_fuss_mitte_gerade
set_class_anim kungfufoothead	mann.kungfu_fuss_oben_dreh
set_class_anim kungfufootbottom	mann.kungfu_fuss_unten_gerade

set_class_anim kungfuhandmiddle	mann.kungfu_hand_mitte_gerade
set_class_anim kungfuhandhead	mann.kungfu_hand_oben_gerade

set_class_anim kungfubackhitl	mann.kungfu_hinten_get_leicht
set_class_anim kungfubackhitm	mann.kungfu_hinten_get_mittel
set_class_anim kungfubackhith	mann.kungfu_hinten_get_schwer
set_class_anim kungfubackhitd	mann.kungfu_hinten_get_tot

set_class_anim kungfumiddleblo	mann.kungfu_mitte_blo

set_class_anim kungfumiddlehitl	mann.kungfu_mitte_get_leicht
set_class_anim kungfumiddlehitm	mann.kungfu_mitte_get_mittel
set_class_anim kungfumiddlehith	mann.kungfu_mitte_get_schwer
set_class_anim kungfumiddlehitd	mann.kungfu_mitte_get_tot

set_class_anim kungfuheadblo	mann.kungfu_oben_blo

set_class_anim kungfuheadhitl	mann.kungfu_oben_get_leicht
set_class_anim kungfuheadhitm	mann.kungfu_oben_get_mittel
set_class_anim kungfuheadhith	mann.kungfu_oben_get_schwer
set_class_anim kungfuheadhitd	mann.kungfu_oben_get_tot

set_class_anim kungfujumpmiddle	mann.kungfu_sprung_mitte
set_class_anim kungfujumphead	mann.kungfu_sprung_oben
set_class_anim kungfujumpbottom	mann.kungfu_sprung_unten

set_class_anim kungfubottomblo	mann.kungfu_unten_blo

set_class_anim kungfubottomhitl	mann.kungfu_unten_get_leicht
set_class_anim kungfubottomhitm	mann.kungfu_unten_get_mittel
set_class_anim kungfubottomhith	mann.kungfu_unten_get_schwer
set_class_anim kungfubottomhitd	mann.kungfu_unten_get_tot

set_class_anim swordduck	mann.schwert_ausw_duck
set_class_anim swordjump	mann.schwert_ausw_jump
set_class_anim swordside	mann.schwert_ausw_seite
set_class_anim swordback	mann.schwert_ausw_zurueck

set_class_anim swordhitheavyb	mann.schwert_ausw_schwer_b
set_class_anim swordsideb	mann.schwert_ausw_seite_b
set_class_anim swordhitdeadb	mann.schwert_ausw_tot_b

set_class_anim swordbackhitl	mann.schwert_hinten_get_leicht
set_class_anim swordbackhitm	mann.schwert_hinten_get_mittel
set_class_anim swordbackhith	mann.schwert_hinten_get_schwer
set_class_anim swordbackhitd	mann.schwert_hinten_get_tot

set_class_anim swordmiddleblo	mann.schwert_mitte_blo

set_class_anim swordmiddlehitl	mann.schwert_mitte_get_leicht
set_class_anim swordmiddlehitm	mann.schwert_mitte_get_mittel
set_class_anim swordmiddlehith	mann.schwert_mitte_get_schwer
set_class_anim swordmiddlehitd	mann.schwert_mitte_get_tot

set_class_anim swordmidstroke	mann.schwert_mitte_hieb
set_class_anim swordmidstab	mann.schwert_mitte_stech

set_class_anim swordheadblo	mann.schwert_oben_blo

set_class_anim swordheadhitl	mann.schwert_oben_get_leicht
set_class_anim swordheadhitm	mann.schwert_oben_get_mittel
set_class_anim swordheadhith	mann.schwert_oben_get_schwer
set_class_anim swordheadhitd	mann.schwert_oben_get_tot

set_class_anim swordheadstroke	mann.schwert_oben_hieb
set_class_anim swordmasterstroke	mann.schwert_oben_meisterhieb
set_class_anim swordheadstab	mann.schwert_oben_stech

set_class_anim swordupstroke	mann.schwert_rauf_hieb
set_class_anim sworddownstroke	mann.schwert_runter_hieb

set_class_anim swordbottomblo	mann.schwert_unten_blo

set_class_anim swordbottomhitl	mann.schwert_unten_get_leicht
set_class_anim swordbottomhitm	mann.schwert_unten_get_mittel
set_class_anim swordbottomhith	mann.schwert_unten_get_schwer
set_class_anim swordbottomhitd	mann.schwert_unten_get_tot

set_class_anim swordbotstroke	mann.schwert_unten_hieb
set_class_anim swordbotstab	mann.schwert_unten_stech

set_class_anim shieldmiddleblo	mann.schild_mitte_blo
set_class_anim shieldheadblo	mann.schild_oben_blo
set_class_anim shieldbottomblo	mann.schild_unten_blo

set_class_anim twohandjump	mann.zweihand_ausw_jump

set_class_anim twohandsideb	mann.zweihand_ausw_seite_b
set_class_anim twohandhitheavyb	mann.zweihand_ausw_schwer_b
set_class_anim twohandhitdeadb	mann.zweihand_ausw_tot_b

set_class_anim twohandbackhitl	mann.zweihand_hinten_get_leicht
set_class_anim twohandbackhitm	mann.zweihand_hinten_get_mittel
set_class_anim twohandbackhith	mann.zweihand_hinten_get_schwer
set_class_anim twohandbackhitd	mann.zweihand_hinten_get_tot

set_class_anim twohandmiddleblo	mann.zweihand_mitte_blo

set_class_anim twohandmidhitl	mann.zweihand_mitte_get_leicht
set_class_anim twohandmidhitm	mann.zweihand_mitte_get_mittel
set_class_anim twohandmidhith	mann.zweihand_mitte_get_schwer
set_class_anim twohandmidhitd	mann.zweihand_mitte_get_tot

set_class_anim twohandmidstroke	mann.zweihand_mitte_hieb

set_class_anim twohandheadblo	mann.zweihand_oben_blo

set_class_anim twohandtwistroke	mann.zweihand_oben_drehhieb

set_class_anim twohandheadhitl	mann.zweihand_oben_get_leicht
set_class_anim twohandheadhitm	mann.zweihand_oben_get_mittel
set_class_anim twohandheadhith	mann.zweihand_oben_get_schwer
set_class_anim twohandheadhitd	mann.zweihand_oben_get_tot

set_class_anim twohandheadstroke	mann.zweihand_oben_hieb
set_class_anim twohandheaddownblo	mann.zweihand_oben_runterblock
set_class_anim twohandupstroke	mann.zweihand_rauf_hieb
set_class_anim twohanddownstroke	mann.zweihand_runter_hieb

set_class_anim twohandbottomblo	mann.zweihand_unten_blo

set_class_anim twohandbothitl	mann.zweihand_unten_get_leicht
set_class_anim twohandbothitm	mann.zweihand_unten_get_mittel
set_class_anim twohandbothith	mann.zweihand_unten_get_schwer
set_class_anim twohandbothitd	mann.zweihand_unten_get_tot

set_class_anim twohandbotstroke	mann.zweihand_unten_hieb

set_class_anim fenrisjump		mann.fenrir_hauen_start
set_class_anim fenrishold		mann.fenrir_hauen_loop
set_class_anim fenrisdown		mann.fenrir_hauen_end
set_class_anim fenrispoison		mann.fenris_gift

// *******************************************************************
// Walk-Animationssets
// *******************************************************************

// 0 Definition des Standard-Walks

set ANIMSET_STANDARDWALK 0
member ANIMSET_STANDARDWALK
set_class_animset $ANIMSET_STANDARDWALK {
	{standard			mann.standard				}
	{walk_start			mann.gehen_start			}
	{walk_loop			mann.gehen_loop				}
	{walk_loop_wave		mann.gehen_gruessen			}
	{walk_stop			mann.gehen_end				}
//	{walk_begin			mann.drogen_nehmen			}
//	{walk_end			mann.sterben				}

	{turn_left_90		mann.drehen_links			}
	{turn_right_90		mann.drehen_rechts			}
	{turn_left_180		mann.drehen_ganz_links		}
	{turn_right_180		mann.drehen_ganz			}

	{climb_standard		mann.kletterstand			}
	{climb_up			mann.kletter_hoch_loop		}
	{climb_down			mann.kletter_runter_loop	}
	{climb_right		mann.kletter_rechts_loop	}
	{climb_left			mann.kletter_links_loop		}

	{ladder_climb_up	mann.leiter_hoch_loop		}
	{ladder_climb_down	mann.leiter_runter_loop		}
	{ground_to_wall		mann.stand_zu_kletterstand	}
	{wall_to_ground		mann.kletterstand_zu_stand	}
//	{ground_to_ladder	mann.						}
	{ladder_to_ground	mann.leiter_zu_boden		}
}

// 1 Definition des Rennen-Walks

set ANIMSET_RUN 1
member ANIMSET_RUN
set_class_animset $ANIMSET_RUN {
	{standard			mann.standard				}
	{walk_start			mann.laufen_schnell_start	}
	{walk_loop			mann.laufen_schnell_loop	}
	{walk_loop_wave		mann.laufen_schnell_loop	}
	{walk_stop			mann.laufen_schnell_end		}

	{turn_left_90		mann.drehen_links			}
	{turn_right_90		mann.drehen_rechts			}
	{turn_left_180		mann.drehen_ganz_links		}
	{turn_right_180		mann.drehen_ganz			}

	{climb_standard		mann.kletterstand			}
	{climb_up			mann.kletter_hoch_schnell	}
	{climb_down			mann.kletter_runter_schnell	}
	{climb_right		mann.kletter_rechts_schnell	}
	{climb_left			mann.kletter_links_schnell	}

	{ladder_climb_up	mann.leiter_hoch_loop		}
	{ladder_climb_down	mann.leiter_runter_loop		}
	{ground_to_wall		mann.stand_zu_kletterstand	}
	{wall_to_ground		mann.kletterstand_zu_stand	}
//	{ground_to_ladder	mann.}
	{ladder_to_ground	mann.leiter_zu_boden		}
}

// 2 Definition des Müde-Walks

set ANIMSET_WALKTIRED 2
member ANIMSET_WALKTIRED
set_class_animset $ANIMSET_WALKTIRED {
	{standard			mann.stand_anim_muede		}
	{walk_start			mann.gehen_muede_start		}
	{walk_loop			mann.gehen_muede_loop		}
	{walk_loop_wave		mann.gehen_muede_gruessen	}
	{walk_stop			mann.gehen_muede_end		}

	{turn_left_90		mann.drehen_links_muede		}
	{turn_right_90		mann.drehen_rechts_muede	}
	{turn_left_180		mann.drehen_links_muede		}
	{turn_right_180		mann.drehen_rechts_muede	}

	{climb_standard		mann.kletterstand			}
	{climb_up			mann.kletter_hoch_schnell	}
	{climb_down			mann.kletter_runter_schnell	}
	{climb_right		mann.kletter_rechts_schnell	}
	{climb_left			mann.kletter_links_schnell	}

	{ladder_climb_up	mann.leiter_hoch_loop		}
	{ladder_climb_down	mann.leiter_runter_loop		}
	{ground_to_wall		mann.stand_zu_kletterstand	}
	{wall_to_ground		mann.kletterstand_zu_stand	}
//	{ground_to_ladder	mann.}
	{ladder_to_ground	mann.leiter_zu_boden		}
}


// 3 Definition des Schwimmen-Walks
set ANIMSET_SWIM 3
member ANIMSET_SWIM
set_class_animset $ANIMSET_SWIM {
	{standard			mann.standard				}
	{walk_start			mann.schwimmen_start		}
	{walk_loop			mann.schwimmen_loop			}
	{walk_loop_wave		mann.schwimmen_loop			}
	{walk_stop			mann.schwimmen_end			}

	{turn_left_90		mann.schwimmen_loop			}
	{turn_right_90		mann.schwimmen_loop			}
	{turn_left_180		mann.schwimmen_loop			}
	{turn_right_180		mann.schwimmen_loop			}

	{climb_standard		mann.schwimmen_wand_stand	}
	{climb_up			mann.schwimmen_hoch			}
	{climb_down			mann.schwimmen_runter		}
	{climb_right		mann.schwimmen_hoch			}
	{climb_left			mann.schwimmen_hoch			}

	{ladder_climb_up	mann.leiter_hoch_loop		}
	{ladder_climb_down	mann.leiter_runter_loop		}
	{ground_to_wall		mann.stand_zu_kletterstand	}
	{wall_to_ground		mann.kletterstand_zu_stand	}
//	{ground_to_ladder	mann.}
	{ladder_to_ground	mann.leiter_zu_boden		}
}


// 4 Definition des Kiepen-Walks
set ANIMSET_WALKWITHPANNIER 4
member ANIMSET_WALKWITHPANNIER
set_class_animset $ANIMSET_WALKWITHPANNIER {
	{standard			mann.standard				}
	{walk_start			mann.gehen_kiepe_start	}
	{walk_loop			mann.gehen_kiepe_loop		}
	{walk_loop_wave		mann.gehen_kiepe_loop		}
	{walk_stop			mann.gehen_kiepe_end		}

	{turn_left_90		mann.drehen_links			}
	{turn_right_90		mann.drehen_rechts			}
	{turn_left_180		mann.drehen_ganz_links		}
	{turn_right_180		mann.drehen_ganz			}

	{climb_standard		mann.kletterstand			}
	{climb_up			mann.kletter_hoch_loop		}
	{climb_down			mann.kletter_runter_loop	}
	{climb_right		mann.kletter_rechts_loop	}
	{climb_left			mann.kletter_links_loop		}

	{ladder_climb_up	mann.leiter_hoch_loop		}
	{ladder_climb_down	mann.leiter_runter_loop		}
	{ground_to_wall		mann.stand_zu_kletterstand	}
	{wall_to_ground		mann.kletterstand_zu_stand	}
//	{ground_to_ladder	mann.						}
	{ladder_to_ground	mann.leiter_zu_boden		}
}


// 5 Definition des Hamsterreitens

set ANIMSET_HAMSTERRIDE 5
member ANIMSET_HAMSTERRIDE
set_class_animset $ANIMSET_HAMSTERRIDE {
	{standard			mann.standard				}
	{walk_start			mann.hamster_start			}
	{walk_loop			mann.hamster_loop			}
	{walk_loop_wave		mann.hamster_loop			}
	{walk_stop			mann.hamster_end			}
	{walk_begin			mann.hamster_aufsteigen		}
	{walk_end			mann.hamster_absteigen		}

	{turn_left_90		mann.hamster_drehen_links	}
	{turn_right_90		mann.hamster_drehen_rechts	}
	{turn_left_180		mann.hamster_drehen_ganz	}
	{turn_right_180		mann.hamster_drehen_ganz	}

	{climb_standard		mann.kletterstand			}
	{climb_up			mann.kletter_hoch_schnell	}
	{climb_down			mann.kletter_runter_schnell	}
	{climb_right		mann.kletter_rechts_schnell	}
	{climb_left			mann.kletter_links_schnell	}

	{ladder_climb_up	mann.leiter_hoch_loop		}
	{ladder_climb_down	mann.leiter_runter_loop		}
	{ground_to_wall		mann.stand_zu_kletterstand	}
	{wall_to_ground		mann.kletterstand_zu_stand	}
//	{ground_to_ladder	mann.						}
	{ladder_to_ground	mann.leiter_zu_boden		}
}


// 6 Definition des Hoverboards

set ANIMSET_HOVERBOARD 6
member ANIMSET_HOVERBOARD
set_class_animset $ANIMSET_HOVERBOARD {
	{standard			mann.board_stand			}
	{walk_start			mann.board_start			}
	{walk_loop			mann.board_loop				}
	{walk_loop_wave		mann.board_loop				}
	{walk_stop			mann.board_end				}
	{walk_begin			mann.board_aufsteigen		}
	{walk_end			mann.board_absteigen		}

	{turn_left_90		mann.board_drehen_ganz		}
	{turn_right_90		mann.board_drehen_ganz		}
	{turn_left_180		mann.board_drehen_ganz		}
	{turn_right_180		mann.board_drehen_ganz		}

	{climb_standard		mann.kletterstand			}
	{climb_up			mann.kletter_hoch_schnell	}
	{climb_down			mann.kletter_runter_schnell	}
	{climb_right		mann.kletter_rechts_schnell	}
	{climb_left			mann.kletter_links_schnell	}

	{ladder_climb_up	mann.leiter_hoch_loop		}
	{ladder_climb_down	mann.leiter_runter_loop		}
	{ground_to_wall		mann.stand_zu_kletterstand	}
	{wall_to_ground		mann.kletterstand_zu_stand	}
//	{ground_to_ladder	mann.						}
	{ladder_to_ground	mann.leiter_zu_boden		}
}


// 7 Definition des Kistentragens

set ANIMSET_WALKWITHBOX 7
member ANIMSET_WALKWITHBOX
set_class_animset $ANIMSET_WALKWITHBOX {
	{standard			mann.kiste_stand			}
	{walk_start			mann.gehen_kiste_start		}
	{walk_loop			mann.gehen_kiste_loop		}
	{walk_loop_wave		mann.gehen_kiste_loop		}
	{walk_stop			mann.gehen_kiste_end		}

	{turn_left_90		mann.kiste_drehen_links		}
	{turn_right_90		mann.kiste_drehen_rechts	}
	{turn_left_180		mann.kiste_drehen_links		}
	{turn_right_180		mann.kiste_drehen_rechts	}

	{climb_standard		mann.kletterstand			}
	{climb_up			mann.kletter_hoch_loop		}
	{climb_down			mann.kletter_runter_loop	}
	{climb_right		mann.kletter_rechts_loop	}
	{climb_left			mann.kletter_links_loop		}

	{ladder_climb_up	mann.leiter_hoch_loop		}
	{ladder_climb_down	mann.leiter_runter_loop		}
	{ground_to_wall		mann.stand_zu_kletterstand	}
	{wall_to_ground		mann.kletterstand_zu_stand	}
//	{ground_to_ladder	mann.						}
	{ladder_to_ground	mann.leiter_zu_boden		}
}


// 8 Definition des Sneak-Walks

set ANIMSET_SNEAK 8
member ANIMSET_SNEAK
set_class_animset $ANIMSET_SNEAK {
	{walk_start			mann.schleichen_start	}
	{walk_loop			mann.schleichen_loop	}
	{walk_stop			mann.schleichen_end	}
}


// 9 Definition des Flee-Walks

set ANIMSET_FLEE 9
member ANIMSET_FLEE
set_class_animset $ANIMSET_FLEE {
	{walk_start			mann.fliehen_start	}
	{walk_loop			mann.fliehen_loop	}
	{walk_stop			mann.fliehen_end	}
}


// 10 Definition des Zombie-Walks

set ANIMSET_ZOMBIE 10
member ANIMSET_ZOMBIE
set_class_animset $ANIMSET_ZOMBIE {
	{walk_start			mann.gehen_zombie_start	}
	{walk_loop			mann.gehen_zombie_loop	}
	{walk_stop			mann.gehen_zombie_end	}
}


// 11 Definition des Taucher-Walks

set ANIMSET_DIVER 11
member ANIMSET_DIVER
set_class_animset $ANIMSET_DIVER {
	{walk_start			mann.tauchen_start	}
	{walk_loop			mann.tauchen_loop	}
	{walk_stop			mann.tauchen_end	}
}

// 12 Definition des Hoppsalauf-Walks

set ANIMSET_SKIPPWALK 12
member ANIMSET_SKIPPWALK
set_class_animset $ANIMSET_SKIPPWALK {
    {standard   mann.standard}
    {walk_start  mann.skipp_start}
    {walk_loop   mann.skipp_loop }
    {walk_stop   mann.skipp_end }
}

// 13 Definition des Betrunken-Walks

set ANIMSET_DRUNKENWALK 13
member ANIMSET_DRUNKENWALK
set_class_animset $ANIMSET_DRUNKENWALK {
    {standard   mann.standard}
    {walk_start mann.gehen_betrunken_start }
    {walk_loop  mann.gehen_betrunken_loop}
    {walk_stop  mann.gehen_betrunken_end}
}


// 14 Definition des Walkfit_Walks

set ANIMSET_WALKFIT 14
member ANIMSET_WALKFIT
set_class_animset $ANIMSET_WALKFIT {
	{walk_start			mann.gehen_fit_start	}
	{walk_loop			mann.gehen_fit_loop	}
	{walk_stop			mann.gehen_fit_end	}
}


// 15 Definition des Fassrollen Walks

set ANIMSET_BARRELWALK 15
member ANIMSET_BARRELWALK
set_class_animset $ANIMSET_BARRELWALK {
	{walk_start			mann.rollen_fass_start}
	{walk_loop			mann.rollen_fass_loop}
	{walk_stop			mann.rollen_fass_end}
}

// 16 Definition des Streik-Walks

set ANIMSET_WALKSTRIKE 16
member ANIMSET_WALKSTRIKE
set_class_animset $ANIMSET_WALKSTRIKE {
    {standard   		mann.streiken_standanim}
	{walk_start			mann.gehen_streiken_start}
	{walk_loop			mann.gehen_streiken_loop}
	{walk_stop			mann.gehen_streiken_end}

	{turn_left_90		mann.drehen_streiken}
	{turn_right_90		mann.drehen_streiken}
	{turn_left_180		mann.drehen_streiken}
	{turn_right_180		mann.drehen_streiken}

}

// Fight Animsets
set ANIMSET_FIGHTKUNGFU 256
member ANIMSET_FIGHTKUNGFU
set_class_animset $ANIMSET_FIGHTKUNGFU {
	{walk_start			mann.laufen_kungfu_start	}
	{walk_loop			mann.laufen_kungfu_loop		}
	{walk_loop_wave		mann.laufen_kungfu_loop		}
	{walk_stop			mann.laufen_kungfu_end		}

	{turn_left_90		mann.kungfu_drehen_ganz_a	}
	{turn_right_90		mann.kungfu_drehen_ganz_b	}
	{turn_left_180		mann.kungfu_drehen_ganz_a	}
	{turn_right_180		mann.kungfu_drehen_ganz_b	}
}

set ANIMSET_FIGHTSWORD 257
member ANIMSET_FIGHTSWORD
set_class_animset $ANIMSET_FIGHTSWORD {
	{walk_start			mann.laufen_schwert_start	}
	{walk_loop			mann.laufen_schwert_loop	}
	{walk_loop_wave		mann.laufen_schwert_loop	}
	{walk_stop			mann.laufen_schwert_end		}
}

set ANIMSET_FIGHTTWOHAND 258
member ANIMSET_FIGHTTWOHAND
set_class_animset $ANIMSET_FIGHTTWOHAND {
	{walk_start			mann.laufen_zweihand_start	}
	{walk_loop			mann.laufen_zweihand_loop	}
	{walk_loop_wave		mann.laufen_zweihand_loop	}
	{walk_stop			mann.laufen_zweihand_end	}
}

