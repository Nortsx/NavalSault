# Naval Artillery Strategy Game - Design Document

## High Concept
A turn-based tactical naval combat game focused on reconnaissance, ballistic artillery combat, and strategic resource management. Players command small fleets (5-10 ships) in intense battles where information warfare is as crucial as firepower.

---

## Core Pillars

### 1. Reconnaissance is Power
- Vision is everything - without recon, you're shooting blind
- Recon aircraft are vulnerable and must be protected
- Fog of war creates tension and forces calculated risks

### 2. Ballistic Precision
- Every shot considers range, accuracy radius, and positioning
- Different weapon systems offer tactical trade-offs
- Blind firing is possible but risky

### 3. Action Economy
- Every decision costs action points
- Heavier, more powerful ships cost more to activate
- Resource management is key to victory

---

## Core Gameplay Loop

### Turn Structure
**Planning Phase:**
1. Player receives action points for the turn
2. Player plans all actions for their fleet:
   - Ship movements (costs vary by ship type)
   - Artillery shots (costs points)
   - Special ability activations (costs points)
3. Actions are queued in specific order chosen by player

**Execution Phase:**
1. All planned actions execute in the order queued
2. Player actions execute first, then enemy actions
3. Results are displayed (hits, misses, damage, destroyed equipment)
4. Turn ends, new turn begins

### Action Points System
- Each turn grants a fixed pool of action points
- All actions (movement, shooting, abilities) cost points
- Heavier/more powerful ships cost more points to activate
- Points do NOT carry over between turns (use it or lose it)
- Strategic choice: activate fewer powerful ships or more weaker ones

---

## Combat Mechanics

### Shooting System
**Targeting:**
- Can target specific coordinates (even if no ship visible)
- Can target last known position of enemy ships
- Can fire completely blind as a gamble

**Accuracy:**
- Each ship has an accuracy radius around the target point
- Shot can hit anywhere within this radius
- Accuracy degrades with range
- Factors affecting accuracy:
  - Distance to target
  - Ship's inherent accuracy stat
  - Whether target has been recon'd or not

**Hit Resolution:**
- Shell lands within accuracy radius
- If enemy ship is in landing zone, it takes damage
- No feedback on blind shots - you won't know if you hit until recon confirms

**Damage Types:**
- **Standard Shells:** Single target, high damage
- **Splash Shells:** Area of effect, lower damage per ship
  - Hits multiple ships if clustered
  - Useful against grouped formations
  - Trade-off: less reliable single-target damage

### Ship Health & Damage
- Ships have hit points (HP)
- Taking damage can destroy special equipment:
  - Weapons systems
  - Recon aircraft
  - Special abilities (repair, speed boost, etc.)
- Destroyed equipment cannot be used until repaired
- Ships sink when HP reaches 0

---

## Reconnaissance System

### Recon Aircraft
- Launched from specialized recon ships
- Must be sent to specific areas to reveal enemy positions
- Can be shot down by enemy fire or AA-specialized ships
- Recon ship ability can be destroyed by enemy fire

### Fog of War
**With Active Recon:**
- Enemy ships fully visible in recon'd areas
- Can target with full accuracy
- Real-time position updates

**Without Recon:**
- Only see last known positions of enemy ships
- Enemy may have moved since last spotted
- Must guess/predict enemy movements
- Shooting at last known positions is high-risk gamble

**Tactical Implications:**
- Protecting recon ships is critical
- Losing recon cripples your effectiveness
- Creates information asymmetry opportunities
- Timing recon flights with artillery strikes is crucial

---

## Ship Design Framework

### Ship Attributes
Every ship has the following core stats:

**Combat Stats:**
- Hit Points (HP)
- Maximum Range (how far it can shoot)
- Accuracy Radius (spread of shots)
- Damage Output
- Splash Damage (yes/no + radius if yes)

**Movement:**
- Action Point Cost (heavier ships cost more)
- Movement Speed/Range per activation

**Special Equipment:**
- Primary Ability (if any)
- Equipment HP (can be destroyed separately from ship)

### Planned Ship Archetypes

**Heavy Artillery Ships:**
- High HP, high damage
- Long maximum range
- Expensive action point cost
- Poor accuracy radius (high spread)
- No splash damage
- Role: Long-range bombardment

**Splash Artillery Ships:**
- Medium HP, medium damage
- Medium range
- Medium action point cost
- Splash damage capability
- Lower single-target damage than heavy artillery
- Role: Area denial, grouped target engagement

**Recon Carriers:**
- Low-medium HP
- Special Ability: Deploy recon aircraft
- Minimal or no offensive weapons
- Low-medium action point cost
- Role: Intelligence gathering, vision control

**Support Ships:**
- Various HP levels
- Special Abilities such as:
  - **Repair:** Restore HP to friendly ships mid-battle
  - **Speed Boost:** Reduce action point cost for target ship
  - **Other utility abilities** (TBD during playtesting)
- Role: Force multiplication, sustainability

**Fast Attack Ships:**
- Low HP
- Short-medium range
- Low action point cost
- Fast movement
- Role: Flanking, harassment, quick strikes

**AA Specialist Ships:**
- Medium HP
- Special: High effectiveness against recon aircraft
- Can shoot down planes more reliably
- Role: Denying enemy intelligence

### Design Philosophy
- Ship classes are NOT rigidly defined yet
- Stats and abilities will be balanced through playtesting
- Each ship should offer meaningful tactical choices
- Composition variety should be encouraged

---

## Special Abilities System

### Ability Mechanics
- Abilities cost action points to use
- Equipment can be destroyed by enemy fire
- Destroyed abilities cannot be used until repaired
- Multiple ships can have the same ability type

### Confirmed Abilities

**Repair:**
- Restores HP to target friendly ship
- Can target self or other ships
- Amount of HP restored TBD (balance dependent)

**Speed Boost:**
- Reduces action point cost for target ship
- Duration TBD (single turn or multiple)
- Stacking behavior TBD

**Recon Aircraft:**
- Deploys aircraft to reveal area
- Can be shot down
- Duration/coverage area TBD

### Future Ability Ideas (To Be Tested)
- Smoke screens (block vision)
- Enhanced targeting (improve accuracy)
- Emergency evasion (dodge incoming fire)
- Torpedo attacks (different attack pattern)
- Electronic warfare (disrupt enemy abilities)

---

## Campaign Structure

### Mission-Based Progression
- Single-player story campaign
- Linear mission progression
- Each mission has pre-set fleet composition
- No persistent fleet between missions (for now)

### Mission Variety

**Objective Types:**

1. **Escort Mission**
   - Protect friendly convoy ships
   - Convoy must reach destination
   - Victory: Convoy survives
   - Defeat: Too many convoy ships lost

2. **Base Capture**
   - Secure enemy naval base/position
   - May require holding area for X turns
   - Enemy reinforcements possible
   - Victory: Base captured and held
   
3. **Fleet Defense**
   - Protect friendly fleet from enemy assault
   - Friendly AI fleet needs to survive
   - You provide fire support and screening
   - Victory: Friendly fleet survives with acceptable losses

4. **Search & Destroy**
   - Eliminate all enemy ships
   - Classic naval engagement
   - Victory: Enemy fleet eliminated

5. **Breakthrough**
   - Your fleet must reach exit point
   - Enemy tries to prevent escape
   - Victory: Enough ships escape

6. **Area Denial**
   - Prevent enemy from reaching objective
   - Hold them off for X turns
   - Victory: Time limit reached with objective intact

### Mission Design Considerations
- Pre-set fleets test different compositions
- Gradually introduce ship types and abilities
- Increase complexity and challenge over time
- Mix objective types to maintain variety
- Optional secondary objectives for replayability

---

## Setting & Aesthetic

### Visual Direction
- **3D graphics** with top-down strategic camera
- **Aggressive ship designs** - imposing, powerful aesthetics
- Fictional universe allows creative freedom
- Ships should look distinct and recognizable
- Effects should clearly communicate hits, misses, explosions

### Art Style References (Placeholder)
- Bold, chunky ship silhouettes
- Clear visual feedback for game state
- Readable at strategic zoom level
- Dramatic muzzle flashes and shell trails
- Satisfying explosion effects

### Setting
- Fictional naval conflict
- Allows freedom in ship design without historical constraints
- Story/faction details TBD
- Focus on tactical gameplay over narrative (initially)

---

## UI/UX Considerations

### Critical Information Display
- Current action points available
- Action point cost for each planned action
- Visible accuracy radius when targeting
- Clear fog of war visualization
- Ship HP and equipment status
- Turn order queue display

### Player Feedback
- Clear hit/miss indicators
- Damage numbers
- Equipment destruction notifications
- Visual distinction between recon'd and unrecon'd areas
- Range indicators for weapons

---

## Balance Considerations (For Playtesting)

### Questions to Answer Through Testing

**Action Point Economy:**
- What's the right base action point pool per turn?
- How much should heavy vs light ships cost?
- Should some missions give bonus/penalty points?

**Combat Balance:**
- Splash damage radius and damage multiplier
- HP pools for different ship types
- Range vs accuracy trade-offs
- Repair ability strength

**Recon Balance:**
- How easy should it be to shoot down recon planes?
- Should recon coverage be permanent or temporary?
- Recon range and deployment time

**Ability Balance:**
- Action point costs for abilities
- Cooldowns vs point costs
- Power level of support abilities
- Equipment destruction rates

---

## Development Priorities

### Phase 1: Core Prototype
- [ ] Basic turn structure and action point system
- [ ] Simple movement on grid/map
- [ ] Basic shooting mechanics with accuracy radius
- [ ] Hit detection and damage
- [ ] 2-3 basic ship types for testing

### Phase 2: Reconnaissance
- [ ] Fog of war implementation
- [ ] Recon ship and aircraft mechanics
- [ ] Last-known position tracking
- [ ] Blind-fire mechanics

### Phase 3: Ship Variety
- [ ] Implement splash damage
- [ ] Special abilities system
- [ ] Equipment destruction mechanics
- [ ] 5-6 distinct ship types
- [ ] Support ships and abilities

### Phase 4: Mission Framework
- [ ] Victory condition system
- [ ] Mission objectives (escort, capture, etc.)
- [ ] Pre-set fleet loading
- [ ] Basic enemy AI

### Phase 5: Campaign & Polish
- [ ] Story missions with progression
- [ ] Tutorial/onboarding
- [ ] UI/UX polish
- [ ] 3D art integration
- [ ] Sound and visual effects
- [ ] Balancing pass

---

## Open Questions & Design Challenges

### To Be Resolved

1. **Map Design:**
   - Pure open ocean or islands/terrain?
   - Map size relative to weapon ranges?
   - Environmental effects (weather, currents)?

2. **Enemy AI:**
   - How sophisticated should enemy planning be?
   - Should enemies also have action point limits?
   - Difficulty scaling approach?

3. **Ship Customization:**
   - Fixed ship loadouts or customizable before missions?
   - Equipment upgrades between missions?
   - Player choice in fleet composition?

4. **Meta Progression:**
   - Unlock new ship types over campaign?
   - Persistent upgrades or bonuses?
   - Multiple difficulty modes?

5. **Multiplayer Potential:**
   - PvP viability with simultaneous planning?
   - Balance considerations for competitive play?
   - Async multiplayer possibilities?

---

## Success Metrics

### Core Experience Goals
- Players should feel clever when prediction pays off
- Losing recon should feel genuinely scary
- Action point decisions should be agonizing
- Blind-fire hits should be thrilling
- Different ship compositions should feel distinct

### Design Validation
- Are players using recon meaningfully?
- Is the action point system creating interesting choices?
- Do abilities feel impactful but not mandatory?
- Is splash damage useful in practice?
- Are missions varied enough?

---

## Next Steps

1. **Prototype core mechanics** (movement, shooting, action points)
2. **Playtest basic combat** to validate feel
3. **Implement fog of war** and gauge impact
4. **Build 3-4 ship types** and test variety
5. **Create first complete mission** end-to-end
6. **Iterate based on playtesting feedback**

---

## Notes & Ideas Parking Lot

- Consider weather effects (wind affecting shell trajectory?)
- Naval mines as environmental hazard?
- Different ammo types (armor-piercing vs HE)?
- Morale/crew system?
- Day/night cycle affecting recon?
- Damaged ships moving slower?
- Critical hits for extra effects?

---

**Document Version:** 1.0  
**Last Updated:** Initial Draft  
**Status:** Ready for Prototyping

