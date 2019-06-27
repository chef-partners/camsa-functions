# Define attributes which will be used in the tests to determine
# where the tests should be run
base_url = attribute('function_url', default: 'http://192.168.50.191:7071/api')

# TEST 1: Ensoure that unknown API endpoints respond with the correct response 
control 'GET unknown endpoint from the API' do
  impact 1.0
  title 'Call unkown API endpoint'

  response = http("#{base_url}/foobar", method: 'GET')
  describe response do
    it 'should return HTTP 404' do
      expect(subject.status).to eq(404)
    end

    it 'should state why returned 404' do
      expect(JSON.parse(subject.body)['message']).to eq('Specified type is not recognised: foobar')
    end
  end
end

# TEST 2: Ensure that a call to the config endpoint meets with 200
control 'GET config endpoint' do
  impact 1.0
  title 'Call /config'

  response = http("#{base_url}/config", method: 'GET')
  describe response do
    it 'should return HTTP 200' do
      expect(subject.status).to eq(200)
    end
  end
end

# TEST 3: Attempt to retrieve unknown item from the database
control 'GET item from config endpoint' do
  impact 1.0
  title 'Retrieve /config/inspec_test'

  response = http("#{base_url}/config/inspec_test", method: 'GET')
  describe response do
    it 'should return HTTP 404' do
      expect(subject.status).to eq(404)
    end

    it 'should state why returned 404' do
      expect(JSON.parse(subject.body)['message']).to eq('Item cannot be found: inspec_test')
    end    
  end
end

# TEST 4: Add item to the config store
control 'POST item to config API' do
  impact 1.0
  title 'Add new item'

  response = http("#{base_url}/config", method: 'POST', data: '{"inspec_test": "it rocks"}')

  describe response do
    it 'should return HTTP 204' do
      expect(subject.status).to eq(204)

    end
  end
end

# TEST 5: Attempt to rerieve the POST'ed data
control 'Retrieve newly inserted data' do
  impact 1.0
  title 'GET inspec_test'

  response = http("#{base_url}/config/inspec_test", method: 'GET')

  describe response do
    it 'should return HTTP 200' do
      expect(subject.status).to eq (200)
    end
    it 'inspec_test should have a value of "it rocks"' do
      expect(JSON.parse(subject.body)['inspec_test']).to eq('it rocks')
    end
  end
end

# TEST 6: Ensure that a retrieval of an item against an unknown category returns 404
control 'Retrieve data in category' do
  impact 1.0
  title 'Unknwon category of "inspec"'

  response = http("#{base_url}/config/inspec_test/inspec", method: 'GET')

  describe response do
    it 'should return HTTP 404' do
      expect(subject.status).to eq (404)
    end

    it 'should state why returned 404' do
      expect(JSON.parse(subject.body)['message']).to eq('Item cannot be found: inspec_test')
    end  
  end
end

# TEST 6: Remove item added to the storage
control 'Remove item in api' do
  impact 1.0
  title 'Remove inspec_test'

  response = http("#{base_url}/config/inspec_test", method: 'DELETE')

  describe response do
    it 'should return HTTP 200' do
      expect(subject.status).to eq (200)
    end
  end
end

# TEST 7: Attempt to rerieve the deleted data
control 'Retrieve deleted data' do
  impact 1.0
  title 'GET inspec_test'

  response = http("#{base_url}/config/inspec_test", method: 'GET')

  describe response do
    it 'should return HTTP 404' do
      expect(subject.status).to eq (404)
    end
  end
end
