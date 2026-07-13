// Alle Kommentare mit // einleiten
// sparetalk_de.tcl 	- alles über Freizeit
// *   tlw - zu lange Wege
// *   hun - hungrig sein
// *   pns - niemand in Bar
// *   pfl - Bar voll
// *   pnb - kein Bier
// *   ffl - FStudio voll
// *   bfl - bowling voll
// *   tns - niemand in Theater
// *   tfl - Theater voll
// *   dns - niemand in Disco
// *   dfl - Disco voll
// *   sns - niemand in Bordell
// *   sfl - Bordell voll
// *   kit - Küche voll


// ************************************************************************************************
// *	 In der Freizeit viel rumgelaufen
// ************************************************************************************************
// #############				TECHNO-GRAD NIEDRIG ******************************************
// ****
// **** RELEVANZ:	schwach:
// ****

// Stimmung: GUT			#1
smalltalk add "tlw" {15 7 0.0 20.0 0.0 0.6 1.0
"Maybe I'll get a medal soon. \nI'm constantly spending my time off \nrunning around! That's me!"
}
// Stimmung: NAJA			#2
smalltalk add "tlw" {15 7 0.0 20.0 0.0 0.3 0.7
"I always have to walk so far, \nwhen I just want to relax a little!"
}
// Simmung: MIES			#3
smalltalk add "tlw" {15 7 0.0 20.0 0.0 0.0 0.4
"I'm sure it bugs you, too, that we're \nbeing sent around \nso much in our free time!?"
"No... I still like long walks!"
":Yup, pretty long ones!"
}

// ****
// **** RELEVANZ:	mittel:	*********************
// ****
// Stimmung: GUT			#4
smalltalk add "tlw" {15 7 15.0 35.0 0.0 0.6 1.0
"We shouldn't be complaining...! \nIt's fun to walk around, \nwhen you want to relax, isn't it?"
}
// Stimmung: NAJA			#5
smalltalk add "tlw" {15 7 15.0 35.0 0.0 0.3 0.7
"I tell you! \nI have to walk half a day, \nwhen I want to relax!"
}
// Simmung: MIES			#6
smalltalk add "tlw" {15 7 15.0 35.0 0.0 0.0 0.4
"I've been walking the \nwhole day again, just so \nI can finally relax!!!"
}

// ****
// **** RELEVANZ:	stark:	*********************
// ****
// Stimmung: GUT			#7
smalltalk add "tlw" {15 7 30.0 50.0 0.0 0.6 1.0
"It's such as long way, just \nto relax a little! \nWell, walking can be \nsort of relaxing..."
}
// Stimmung: NAJA			#8
smalltalk add "tlw" {15 7 30.0 50.0 0.0 0.3 0.7
"Yeah right, walking relaxes! \nI always have to walk forever \n before I can get some rest!"
}
// Simmung: MIES			#9
smalltalk add "tlw" {15 7 30.0 50.0 0.0 0.0 0.4
"Right! This sucks! \nTo relax in my valuable free time means for \nme having to walk 20 kilometers first! GREAT!"
}

// #############				TECHNO-GRAD MITTEL ******************************************
// ****
// **** RELEVANZ:	schwach:
// ****

// Stimmung: GUT			#1

// Stimmung: NAJA			#2

// Simmung: MIES			#3

// ****
// **** RELEVANZ:	mittel:	*********************
// ****
// Stimmung: GUT			#4

// Stimmung: NAJA			#5

// Simmung: MIES			#6

// ****
// **** RELEVANZ:	stark:	*********************
// ****
// Stimmung: GUT			#7

// Stimmung: NAJA			#8

// Simmung: MIES			#9
smalltalk add "tlw" {15 7 30.0 50.0 0.2 0.0 0.4
"The little free time we \nhave is there to relax... \nwithout walking 2000 kilometers first!"
}

// #############				TECHNO-GRAD HOCH ******************************************
// ****
// **** RELEVANZ:	schwach:
// ****

// Stimmung: GUT			#1

// Stimmung: NAJA			#2

// Simmung: MIES			#3

// ****
// **** RELEVANZ:	mittel:	*********************
// ****
// Stimmung: GUT			#4

// Stimmung: NAJA			#5

// Simmung: MIES			#6

// ****
// **** RELEVANZ:	stark:	*********************
// ****
// Stimmung: GUT			#7

// Stimmung: NAJA			#8
smalltalk add "tlw" {15 7 30.0 50.0 0.4 0.3 0.7
"Whenever I want to go out, \nall the places are so far away!"
}
// Simmung: MIES			#9
smalltalk add "tlw" {15 7 30.0 50.0 0.4 0.0 0.4
"I CAN'T BELIEVE IT! \nYou get back from work \nand just want to hang out \nat a bar, but it takes you \nan hour to get there!"
}

// ************************************************************************************************
// ************************************************************************************************
// WIGGLE hat HUNGER (spricht NICHT zu Partner)
// ************************************************************************************************
// Geordnet nach 1 = schwach bis 5 = Stark, Kohldampf
// Stimmung: GUT			************************************************************	
// ****	1 **********
smalltalk add "hun" {15 7 0.0 20.0 0.0 0.5 1.0
"Ooops... \nwas that my stomach? \nI should eat!"
":Are you always talking to your stomach?"
":My stomach just burps - but never talks to me'."}
// ****
smalltalk add "hun" {15 7 5.0 20.0 0.0 0.5 1.0
"Am I hungry now or \nis it just a craving? \nWould you like to eat, too?"
":I can always eat!"
":Ok... let's eat something!"
":I don't know... I'm too fat!"
":Well, I'm so fat, no thanks!"
}

// ****	3 **********
smalltalk add "hun" {15 7 10.0 25.0 0.0 0.5 1.0
"Are you hungry, too?"
"Me? I'm always hungry!"
"Me? I always have a craving."
}
// ****
smalltalk add "hun" {15 7 15.0 30.0 0.0 0.5 1.0
"We should chop a few mushrooms - \nI'm hungry."
":Go ahead!"
":Well, then I'll just chop them on my own!"
}
// ****
smalltalk add "hun" {15 7 20.0 35.0 0.0 0.5 1.0
"Wow... I am starving. \nShould we eat some mushrooms?"
}
// ****	5 **********
smalltalk add "hun" {15 7 25.0 40.0 0.0 0.5 1.0
"My stomach starts hurting! \nI'm sooo hungry!"
}
// ****
smalltalk add "hun" {15 7 30.0 45.0 0.0 0.5 1.0
"I could eat an \nentire hamster tribe."
":I'll join in!"
":Ok, let's go beat up some hamsters!"
}
// ****
smalltalk add "hun" {15 7 30.0 50.0 0.0 0.5 1.0
"I'm in desperate need for food! \nEven if it's just worms! \nAre you hungry, too?"
":Sure!"
":Indeed - you're speaking the truth.\n My stomach is rumbling..."
":No... but I could already eat again!"
}
// ****
smalltalk add "hun" {15 7 35.0 50.0 0.0 0.5 1.0
"My stomach is already giving concerts! \nI'm starving! Listen."
":Are you hitting on me?"
":If you unbutton your shirt, I'll run away screaming!!!"
":Excuse me?!?!? What did you say?!?! I can't hear anything!"
}

// Simmung: MIES			************************************************************
// ****	1 **********
smalltalk add "hun" {15 7 0.0 20.0 0.0 0.2 0.5
"Do you have any grilled mushrooms? \nI'm hungry!"
":If I had any, I wouldn't give them to you! Ha!"
":I could already have a few again!"
":No... do you?"
}
// ****
smalltalk add "hun" {15 7 0.0 20.0 0.0 0.0 0.3
"Should we fix something to eat? \nIt's your turn! Hehe..."
":No! Your turn!"
":I always have to cook! Cook it yourself!"
":Well, just eat your cap then!"
":You'll have to invite me, if you wish to dine with me."
}
// ****	3 **********
smalltalk add "hun" {15 7 15.0 35.0 0.0 0.0 0.5
"I'm so hungry, I might pass out - \nwhat about you?"
":All you ever think about is eating!"
":No problems!"
":Hunger? What is that?"
":Me -- I'm ALWAYS hungry!"}

// ****	5 **********
smalltalk add "hun" {15 7 30.0 50.0 0.0 0.0 0.3
"For Odin's sake! I'm \nstarving! Darned!"
":You, too?"
":My stomach is making weird noises!"
":All you ever think about is eating!"
":Yeah, go ahead and eat until you're stuffed!"
}
// ****
smalltalk add "hun" {15 7 30.0 50.0 0.0 0.1 0.5
"Hey, have you seen anything edible around - \nmaybe a hamster?"
":You shouldn't eat hamsters!"
":No, have you?"
":Let's go catch one!"
}
// ****
smalltalk add "hun" {15 7 30.0 50.0 0.0 0.3 0.5
"The next hamster I see, \nI'll eat right away!"
":Yuck! Skin him at least!"
":Gross! I prefer mushrooms!"
":I'll join in, let's split him!"
":You'll remove the eyes first, won't you?!"
}
// ****
smalltalk add "hun" {15 7 40.0 50.0 0.0 0.0 0.3
"GRRRR! \nI AM SO \nHUNGRY!!!!"
":You're right!"
":Come on, now stop yelling!!!"
":You're always hungry! Stop complaining!"
}

// ******************************************************
// Bar besucht - niemand bedient
// ******************************************************
smalltalk add "pns" {15 7 0.0 50.0 0.0 0.0 1.0
"I was at the \nbar today, \nbut no one \nwserved me!"
}
smalltalk add "pns" {15 7 0.0 50.0 0.0 0.0 1.0
"I sat in the \nbar for an hour until \n I noticed, \nthere weren't any waiters! \nI wanted to \nget drunk!"
}
smalltalk add "pns" {15 7 0.0 50.0 0.0 0.0 1.0
"What a great \ncountry this is? \nThere's a bar - \nbut no waiters!"
}
smalltalk add "pns" {15 7 0.0 50.0 0.0 0.0 1.0
"A service society? Haha! \nThere isn't even \na waiter at the bar!"
}
// ******************************************************
// Bar besucht - war voll
// ******************************************************
smalltalk add "pfl" {15 7 0.0 50.0 0.0 0.0 1.0
"The bar was \npacked again!"
}
smalltalk add "pfl" {15 7 0.0 50.0 0.0 0.0 1.0
"Just wanted to \ngo for a drink... \nthe bar was too crowded!"
}
smalltalk add "pfl" {15 7 0.0 50.0 0.0 0.0 1.0
"The bar was really packed today! \nI couldn't get a seat - \nand they \nwouldn't bring me \na beer either!"
}
smalltalk add "pfl" {15 7 0.0 50.0 0.0 0.0 1.0
"I actually like \ncrowded bars, \nbut if you \ncan't even \nfind a seat!"
}
// ******************************************************
// Bar besucht - Bier alle
// ******************************************************
smalltalk add "pnb" {15 7 0.0 50.0 0.0 0.0 1.0
"What kind of bar \nis this? - NO BEER?"
}
smalltalk add "pnb" {15 7 0.0 50.0 0.0 0.0 1.0
"The bar ran out of \nbeer again."
}
smalltalk add "pnb" {15 7 0.0 50.0 0.0 0.0 1.0
"We should drink less beer! \nWe just floated \nthe last keg!"
}
smalltalk add "pnb" {15 7 0.0 50.0 0.0 0.0 1.0
"I suspect the waiter at \nthe bar of drinking \nall the stuff on his own! \nThey ran out of \nbeer again!"
}
// ******************************************************
// Fitness-Studio war voll
// ******************************************************
smalltalk add "ffl" {15 7 0.0 50.0 0.0 0.0 1.0
"I finally decided to work out, \nbut it was too crowded!"
}
smalltalk add "ffl" {15 7 0.0 50.0 0.0 0.0 1.0
"The gym is \nalways packed... \nwell, I don't have \nto sweat then!"
}
smalltalk add "ffl" {15 7 0.0 50.0 0.0 0.0 1.0
"Why do we have \nthat gym thing, \nif it's always packed?"
}
// ******************************************************
// Bowling-Bahn war voll
// ******************************************************
smalltalk add "bfl" {15 7 0.0 50.0 0.0 0.0 1.0
"I said: 'Let's take it \na little easy today.' \nWell, we went in - \nand he said: 'It's crowded.'\n Well, so we took off!"
}
smalltalk add "bfl" {15 7 0.0 50.0 0.0 0.0 1.0
"The bowling alley was \nso packed, that we \ncouldn't even bowl!"
}
smalltalk add "bfl" {15 7 0.0 50.0 0.0 0.0 1.0
"When you want to bowl, \nall lanes are occupied"
}
smalltalk add "bfl" {15 7 0.0 50.0 0.0 0.0 1.0
"What is a bowling alley \ngood for if it's always \npacked with dwarfs!?"
}
// ******************************************************
// Theater nicht besetzt
// ******************************************************
smalltalk add "tns" {15 7 0.0 50.0 0.0 0.0 1.0
"AWESOME! I saw a \nngreat play today: \n'The Silence'!\n There wasn't anyone \nplaying there!"
}
smalltalk add "tns" {15 7 0.0 50.0 0.0 0.0 1.0
"First I thought it \nwas a grotesque play - \nuntil I noticed that \nthere wasn't anyone playing!"
}
smalltalk add "tns" {15 7 0.0 50.0 0.0 0.0 1.0
"Are they all acting \nat home, or why \nisn't anybody \non stage?"
}
// ******************************************************
// Theater voll
// ******************************************************
smalltalk add "tfl" {15 7 0.0 50.0 0.0 0.0 1.0
"The theater was \nfinally crowded! \nBut it was sooo packed...! \nCome on!"
}
smalltalk add "tfl" {15 7 0.0 50.0 0.0 0.0 1.0
"Since when have they \nall become intellectuals?!? \nThe theater was packed!"
}
smalltalk add "tfl" {15 7 0.0 50.0 0.0 0.0 1.0
"If I were an actor, \nI'd be thrilled! \nBut I am not an actor! \nAnd I'd love to go to the theater, \nbut it's just too crowded!"
}

smalltalk add "tfl" {15 7 0.0 50.0 0.0 0.0 1.0
"Who would feel like \nsitting between stinking \nWiggles in a crammed theater? \nThe place is too crowded!"
}
// ******************************************************
// Disco nicht besetzt
// ******************************************************
smalltalk add "dns" {15 7 0.0 50.0 0.0 0.0 1.0
"They don't even have \na DJ at the club!"
}
smalltalk add "dns" {15 7 0.0 50.0 0.0 0.0 1.0
"Maybe they should \nstart playing music \nat the club?"
}

smalltalk add "dns" {15 7 0.0 50.0 0.0 0.0 1.0
"I was at the club today - \nand it sucked! \nNobody was there."
}
smalltalk add "dns" {15 7 0.0 50.0 0.0 0.0 1.0
"No dwarf would \nwork at the club! So \nwhere can we party?"
}
// ******************************************************
// Disco voll
// ******************************************************
smalltalk add "dfl" {15 7 0.0 50.0 0.0 0.0 1.0
"It's really hard \nto get into the club. \nIt's pretty crowded!"
}
smalltalk add "dfl" {15 7 0.0 50.0 0.0 0.0 1.0
"The club is packed, stop \nletting people in!"
}
smalltalk add "dfl" {15 7 0.0 50.0 0.0 0.0 1.0
"I was at the club today - \nand couldn't get in! \nIt was packed!"
}
// ******************************************************
// Bordell nicht besetzt
// ******************************************************
smalltalk add "sns" {15 7 0.0 50.0 0.0 0.0 1.0
"Well... I went to the \nstrip club.... \nbut nobody was there!"
}
smalltalk add "sns" {15 7 0.0 50.0 0.0 0.0 1.0
"If the guests stay away, \nthat's one thing - but \nit's strange not to see anyone working at the strip club!"
}
smalltalk add "sns" {15 7 0.0 50.0 0.0 0.0 1.0
"Maybe they should \nplant turf there, so we \ncould at least play soccer at the place. \nNot a soul inside!"
}
smalltalk add "sns" {15 7 0.0 50.0 0.0 0.0 1.0
"Not a soul inside the \nstrip club - \nnot even... \nwell you know..."
}
// ******************************************************
// Bordell voll
// ******************************************************
smalltalk add "sfl" {15 7 0.0 50.0 0.0 0.0 1.0
"I went to the strip club - \nwell, for a beer of course.\nIt was way too crowded!"
}
smalltalk add "sfl" {15 7 0.0 50.0 0.0 0.0 1.0
"What can you do if the \nstrip club is that packed?!? \nWait outside all \nnight long, or what!"
}
smalltalk add "sfl" {15 7 0.0 50.0 0.0 0.0 1.0
"The strip club was packed... \nwith Wiggles!"
}
smalltalk add "sfl" {15 7 0.0 50.0 0.0 0.0 1.0
"Closed due to congestion!" \nThe strip club?! \nI can't imagine!"
}
smalltalk add "sfl" {15 7 0.0 50.0 0.0 0.0 1.0
"Tell me, what's \nwrong with us...? \nEverybody goes to the \nstrip club! It's so packed, \nyou can't even get in anymore!"
}

// ******************************************************
// Küche voll
// ******************************************************
smalltalk add "???" {15 7 0.0 50.0 0.0 0.0 1.0 0.25
"Are you open for \na little barbeque?! - How busy \nis it at the fireplace?"
":You can cook?"
"No, I can't! \nIt's too crowded at the fire!"
}
smalltalk add "???" {15 7 0.0 50.0 0.0 0.0 1.0 0.2
"People are stepping onto \neach other's feet at the fire!"
":For Odin's sake - \nyou can't even eat!"
"Fireplaces for everone!"
}
smalltalk add "kit" {15 7 0.0 50.0 0.15 0.0 1.0
"All I wanted to do, was \nto prepare some yummy food! \nBut I can't even get to the oven!"
":Why?"
":So you can cook?"
"Well, it's just too crowded!"
}
smalltalk add "kit" {15 7 0.0 50.0 0.2 0.0 1.0
"I'm starving, man! Well... \nso I went into the kitchen to get some barbeque, right?! \nIt was packed! There were about 100 other dwarfs \nstaring at the pots!"
":Right! We need more kitchens!"
}
smalltalk add "kit" {15 7 0.0 50.0 0.25 0.0 1.0
"Seriously! \nAll you want is to get some food, \nbut the kitchen is occupied!"
":At this place, you can't \neven eat properly!"
":Ridiculous! It's terrible!"
":More pots for everyone!"
":Right! We need more kitchens!"
"Seriously!"
}
smalltalk add "kit" {15 7 0.0 50.0 0.3 0.0 1.0
"I surely enjoy eating! \nBut if the kitchen is that \ncrowded, it's no fun!"
":Exactly!"
":Yeah! Let's just stick all of \nthem into the pots!"
":Right! We need more kitchens!"
}


// ******************************************************
// Meine Schlafstätte ist zu unbequem (Technograd)
// ******************************************************
// #############				TECHNO-GRAD NIEDRIG 
smalltalk add "slp" {15 7 0.0 50.0 0.0 0.0 1.0 0.05
"I'm tired of sleeping \non the ground! \nIt sucks!"
}
smalltalk add "slp" {15 7 0.0 50.0 0.0 0.0 1.0 0.1
"Could anyone \ninvent a mushroom cap pillow!? \nI don't sleep well!"
}
smalltalk add "slp" {15 7 0.0 50.0 0.05 0.0 1.0 0.2
"I'm sick of sleeping \nin this tent! \nIt stinks! All I want \nis a normal bed!"
}
smalltalk add "slp" {15 7 0.0 50.0 0.1 0.0 1.0 0.3
"All you talk about \nis a bed!! \nGod! Come on! \nBut you're right, \nI don't sleep well either!"
}
// #############				TECHNO-GRAD MITTEL 
smalltalk add "slp" {15 7 0.0 50.0 0.2 0.0 1.0
"Is your bed so \nuncomfortable, too? \nWe should have \na better one!"
}
smalltalk add "slp" {15 7 0.0 50.0 0.25 0.0 1.0
"I can't sleep! \nMy bedroom just sucks!"
}
smalltalk add "slp" {15 7 0.0 50.0 0.3 0.0 1.0
"So you and your \nsweetie want to... \nwell, you know... \nthen the bed collapses! \nI want a better bed!"
}
smalltalk add "slp" {15 7 0.0 50.0 0.2 0.0 1.0
"Beds - beds? \nWho cares about beds? \nI want to sleep better!"
}
// #############				TECHNO-GRAD HOCH 
smalltalk add "slp" {15 7 0.0 50.0 0.35 0.0 1.0
"My bedroom looks \nlike a troll's!"
}
smalltalk add "slp" {15 7 0.0 50.0 0.4 0.0 1.0
"My cute bed \nis too small for me!"
}
smalltalk add "slp" {15 7 0.0 50.0 0.4 0.0 1.0
"Is your bedroom as \nmonotonous as mine?!? \nHold on, I did not want \nto go to bed with you... \num-oh-hmm-oh..."
}
smalltalk add "slp" {15 7 0.0 50.0 0.4 0.0 1.0
"One could \nextend my \nbedroom a little!... \nHey, you're slavering!"
}
// ******************************************************
// Bade wunsch - Schöneres Bad (Technograd)
// ******************************************************
// #############				TECHNO-GRAD NIEDRIG 
smalltalk add "bth" {15 7 0.0 50.0 0.0 0.5 1.0 0.2
"Oh, I'd love to \nwash myself sometimes!"
}
smalltalk add "bth" {15 7 0.0 50.0 0.0 0.0 0.6 0.2
"Yuck! I smell \nlike an elf! \nMaybe I should wash myself!"
}
smalltalk add "bth" {15 7 0.0 50.0 0.1 0.5 1.0 0.4
"Oh, a bathtub would \nbe wonderful. \nSome hot water \nand time to relax!"
}
smalltalk add "bth" {15 7 0.0 50.0 0.1 0.0 0.6 0.4
"I used to shower \nevery morning! \nWho cares - then I'll just smell."
}
// #############				TECHNO-GRAD MITTEL 
smalltalk add "bth" {15 7 0.0 50.0 0.25 0.0 1.0
"Hmmm... my bathtub \nis so small, \nI can't even \nsplash in there"
}
smalltalk add "bth" {15 7 0.0 50.0 0.3 0.0 1.0
"Well, I would \nactually like to... \nI would love to... well you know!\n In one bathtub! Unfortunately, \nours is too small!"
}
smalltalk add "bth" {15 7 0.0 50.0 0.35 0.0 1.0
"I am dreaming of \na nice, luxurious bathtub."
}
smalltalk add "bth" {15 7 0.0 50.0 0.4 0.0 1.0
"If only I could \nput my perfect little \nbody into a \nbeautiful bathtub..."
":Take a bucket!"
":Put yourself \ninto a bucket!"
":Perfect body... haha!"
}
// #############				TECHNO-GRAD HOCH 
smalltalk add "bth" {15 7 0.0 50.0 0.4 0.7 1.0
"I wish I had a nice tub - \nthen I could bathe, \nand swim, and splash ---"
":Chill out, buddy! \nYou never wash \nyourself anyway!"
":As if you'd \nwash yourself!"
":Oh, a \nluxurious bathtub \nwould be perfect."
}
smalltalk add "bth" {15 7 0.0 50.0 0.4 0.4 0.8
"I don't even \nuse our little \nbathtub anymore - \nit's kind of embarrassing."
}
smalltalk add "bth" {15 7 0.0 50.0 0.4 0.0 0.5
"Right, stone age \nis over?!?! \nI wish I had \na huge and shiny bathtub!"
}
// ******************************************************
// Die Essenauswahl ist zu klein
// ******************************************************
smalltalk add "eat" {15 7 0.0 50.0 0.0 0.8 1.0
"I wish we had \na larger variety of food!"
}
smalltalk add "eat" {15 7 0.0 50.0 0.0 0.6 1.0
"It seems, as if \nour menu is a \nlittle limited."
}
smalltalk add "eat" {15 7 0.0 50.0 0.0 0.3 0.7
"I'm getting sick of \nour food. \nEvery day \nthe same stuff!"
}
smalltalk add "eat" {15 7 0.0 50.0 0.0 0.0 0.7
"What we'll eat today?!??! \nWHY DO YOU EVEN ASK?!? \nThe same as yesterday, and the day before \nyesterday, and the day...!"
}
smalltalk add "eat" {15 7 0.0 50.0 0.0 0.2 0.5
"Everday the same \nfood - Yuck!"
}
smalltalk add "eat" {15 7 0.0 50.0 0.0 0.0 0.4
"I'm sick of it - \nand get sick from it! \nIsn't there anything \nelse I could eat!"
}
// ******************************************************
// Abwechslung in der Freizeit ist zu gering
// ******************************************************
smalltalk add "fun" {15 7 0.0 50.0 0.0 0.0 1.0
"Some time off is \nwonderful, but what can you do, \nwhen there's nothing to do!"
}
smalltalk add "fun" {15 7 0.0 50.0 0.0 0.5 1.0
"Ok, listen! I can't \nconstantly do the same \nthings in my free time! \nLet's build something new!"
}
smalltalk add "fun" {15 7 0.0 50.0 0.0 0.3 0.9
"I don't want to \ntake another nap!? \nThere's nothing fun \nto do here in our free time!"
}
smalltalk add "fun" {15 7 0.0 50.0 0.0 0.1 0.8
"What a life! \nYou dig all day \nand in your free time, \nthere's only one thing to do..."
}
smalltalk add "fun" {15 7 0.0 50.0 0.0 0.0 0.7
"BOOOOORING! There's \nreally nothing we could do \nhere in our free time!"
}
// ******************************************************
// ubt     Ich wurde ständig \nbeim Unterhalten gestört!
// ******************************************************
smalltalk add "ubt" {15 7 0.0 50.0 0.0 0.5 1.0
"My conversations keep \non getting interrupted! \nLet's see, whether it'll work now!"
}
smalltalk add "ubt" {15 7 0.0 50.0 0.0 0.3 1.0
"Why do I always have to leave, \nwhen I start talking to someone?"
}
smalltalk add "ubt" {15 7 0.0 50.0 0.0 0.0 0.7
"Are you also getting the \nfeeling that someone bosses us \naround as soon as we talk?"
}
smalltalk add "ubt" {15 7 0.0 50.0 0.0 0.0 0.5
"I can't finish a \nsingle conversation!"
}
// ******************************************************
// ubr     Stell dir vor: \nWir wollten es uns gerade \ngemütlich machen - du weißt schon - \nda wurde ich weggerufen!
// ******************************************************
smalltalk add "ubr" {15 7 0.0 50.0 0.0 0.4 1.0
"I tell you, \nbeing a Wiggle \nisn't always easy! \nWhen you do something, \nwell you know... - \nyou're sent away!"
}
smalltalk add "ubr" {15 7 0.0 50.0 0.0 0.2 0.8
"For Odin's sake!!! \nEven if you and your sweetheart want \nto.. well.. you know... \nyou get bossed around!"
}
smalltalk add "ubr" {15 7 0.0 50.0 0.0 0.0 0.5
"Enough is enough! \nMy darling and I just \nwant to... well, you know... - \nand you want help NOW!?!"
}

